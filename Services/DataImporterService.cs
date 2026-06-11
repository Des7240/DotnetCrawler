using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DotnetCrawler.Data;
using DotnetCrawler.Entities;

namespace DotnetCrawler.Services
{
    public class ImportRequest
    {
        public string SourcePath { get; set; } = string.Empty;
    }

    public class ImportProgress
    {
        public bool IsRunning { get; set; }
        public int TotalImages { get; set; }
        public int ProcessedImages { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
    }

    public class DataImporterService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly StorageService _storageService;
        
        public ImportProgress Progress { get; private set; } = new ImportProgress();

        public DataImporterService(IServiceScopeFactory scopeFactory, StorageService storageService)
        {
            _scopeFactory = scopeFactory;
            _storageService = storageService;
        }

        public async Task StartImportAsync(ImportRequest request)
        {
            if (Progress.IsRunning) return;
            Progress = new ImportProgress { IsRunning = true, CurrentStatus = "Đang bắt đầu..." };
            await Task.Run(() => ProcessImportAsync(request));
        }

        private async Task ProcessImportAsync(ImportRequest request)
        {
            Console.WriteLine($"=== BẮT ĐẦU NẠP DỮ LIỆU TỪ: {request.SourcePath} ===");
            
            try 
            {
                if (!Directory.Exists(request.SourcePath))
                {
                    Progress.CurrentStatus = $"Lỗi: Thư mục không tồn tại - {request.SourcePath}";
                    Progress.IsRunning = false;
                    return;
                }

                Progress.CurrentStatus = "Đang quét thư mục...";

            var subjectFolderName = new DirectoryInfo(request.SourcePath).Name;
            var subjectCode = subjectFolderName.Split('-')[0].Trim();

            // Lấy hoặc tạo Subject
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Code == subjectCode);
            if (subject == null)
            {
                subject = new Subject { Code = subjectCode };
                db.Subjects.Add(subject);
                await db.SaveChangesAsync();
                Console.WriteLine($"[Importer] Đã tạo môn học mới: {subjectCode}");
            }

            var threadDirs = Directory.GetDirectories(request.SourcePath);
            
            // Count total images
            Progress.CurrentStatus = "Đang đếm số lượng file...";
            foreach (var td in threadDirs) {
                var fs = Directory.GetFiles(td);
                Progress.TotalImages += fs.Count(f => f.EndsWith(".webp") || f.EndsWith(".jpg") || f.EndsWith(".png"));
            }

            foreach (var threadDir in threadDirs)
            {
                var folderName = new DirectoryInfo(threadDir).Name;
                
                string category = "other";
                var folderNameLower = folderName.ToLower();
                if (folderNameLower.Contains("pe-") || folderNameLower.Contains("-pe")) category = "pe";
                else if (folderNameLower.Contains("fe-") || folderNameLower.Contains("-fe") || folderNameLower.Contains("- fe")) category = "fe";
                else if (folderNameLower.Contains("re-") || folderNameLower.Contains("-re") || folderNameLower.Contains("- re")) category = "re";

                var courseThread = await db.Threads.FirstOrDefaultAsync(t => t.Title == folderName && t.SubjectId == subject.Id);
                if (courseThread == null)
                {
                    courseThread = new CourseThread 
                    { 
                        Title = folderName, 
                        SubjectId = subject.Id, 
                        Category = category,
                        Path = $"images/{subjectCode}/{category}/{folderName}"
                    };
                    db.Threads.Add(courseThread);
                    await db.SaveChangesAsync();
                    Console.WriteLine($"[Importer] Đã tạo thread: {folderName}");
                }

                // Xử lý các file trong thư mục này
                var files = Directory.GetFiles(threadDir);
                var imageFiles = files.Where(f => f.EndsWith(".webp") || f.EndsWith(".jpg") || f.EndsWith(".png")).ToList();

                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 2 };

                await Parallel.ForEachAsync(imageFiles, parallelOptions, async (imgFile, ct) =>
                {
                    Progress.CurrentStatus = $"Đang xử lý {folderName}...";
                    var imgName = Path.GetFileName(imgFile);
                    var txtFile = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == Path.GetFileNameWithoutExtension(imgFile) + "_comments" && f.EndsWith(".txt"));

                    // Upload ảnh
                    var storageKey = $"images/{subjectCode}/{courseThread.Category}/{folderName}/{imgName}";
                    string contentType = imgName.EndsWith(".webp") ? "image/webp" : (imgName.EndsWith(".png") ? "image/png" : "image/jpeg");

                    try
                    {
                        using var fileStream = File.OpenRead(imgFile);
                        var isUploaded = await _storageService.UploadImageStreamAsync(storageKey, fileStream, contentType);
                        if (isUploaded)
                        {
                            Console.WriteLine($"[Upload] Đã tải lên Storage: {storageKey}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[Lỗi Upload] {storageKey}: {e.Message}");
                    }

                    // Xử lý bình luận
                    string? bestAnswer = null;
                    var commentsExtracted = new List<Comment>();

                    if (txtFile != null && File.Exists(txtFile))
                    {
                        var lines = await File.ReadAllLinesAsync(txtFile, ct);
                        var ansCounts = new Dictionary<string, int> { {"A",0},{"B",0},{"C",0},{"D",0},{"E",0},{"F",0} };
                        
                        string currentCommentText = "";
                        bool isReadingComment = false;

                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Content:"))
                            {
                                isReadingComment = true;
                                currentCommentText = "";
                                continue;
                            }
                            if (line.StartsWith("------------------------------------------------"))
                            {
                                if (isReadingComment)
                                {
                                    var text = currentCommentText.Trim().Replace("\n", " ");
                                    if (!string.IsNullOrEmpty(text) && !text.Contains("****") && !text.Contains("Mua gói thành viên"))
                                    {
                                        commentsExtracted.Add(new Comment { Text = text, Votes = 1 });

                                        // Phân tích đáp án
                                        var match = Regex.Match(text, @"\b([A-Fa-f])\b", RegexOptions.IgnoreCase);
                                        if (match.Success)
                                        {
                                            var ch = match.Groups[1].Value.ToUpper();
                                            if (ansCounts.ContainsKey(ch)) ansCounts[ch]++;
                                        }
                                        else
                                        {
                                            var matchStart = Regex.Match(text, @"^([A-Fa-f])[\.\:\)]", RegexOptions.IgnoreCase);
                                            if (matchStart.Success)
                                            {
                                                var ch = matchStart.Groups[1].Value.ToUpper();
                                                if (ansCounts.ContainsKey(ch)) ansCounts[ch]++;
                                            }
                                        }
                                    }
                                }
                                isReadingComment = false;
                                continue;
                            }

                            if (isReadingComment)
                            {
                                currentCommentText += line + "\n";
                            }
                        }

                        int maxVotes = 0;
                        foreach (var kv in ansCounts)
                        {
                            if (kv.Value > maxVotes)
                            {
                                maxVotes = kv.Value;
                                bestAnswer = kv.Key;
                            }
                        }
                    }

                    // Lưu vào DB sử dụng context mới cho từng luồng
                    using var loopScope = _scopeFactory.CreateScope();
                    var loopDb = loopScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    
                    var existingQ = await loopDb.Questions.FirstOrDefaultAsync(q => q.CourseThreadId == courseThread.Id && q.ImageUrl == storageKey);
                    if (existingQ == null)
                    {
                        var q = new Question
                        {
                            CourseThreadId = courseThread.Id,
                            ImageUrl = storageKey, // Lưu link storage relative
                            BestAnswer = bestAnswer
                        };
                        loopDb.Questions.Add(q);
                        await loopDb.SaveChangesAsync();

                        foreach (var c in commentsExtracted)
                        {
                            c.QuestionId = q.Id;
                            loopDb.Comments.Add(c);
                        }
                        await loopDb.SaveChangesAsync();
                    }
                    
                    lock(Progress) {
                        Progress.ProcessedImages++;
                    }
                });
            }

            Progress.CurrentStatus = "Đã hoàn tất!";
            Progress.IsRunning = false;
            Console.WriteLine("=== HOÀN TẤT NẠP DỮ LIỆU ===");
            } 
            catch (Exception ex)
            {
                Progress.CurrentStatus = $"Lỗi: {ex.Message}";
                Progress.IsRunning = false;
            }
        }
    }
}
