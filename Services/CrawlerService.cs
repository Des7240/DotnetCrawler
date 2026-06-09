using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using DotnetCrawler.Data;
using DotnetCrawler.Entities;
using Polly;
using System.Text.RegularExpressions;

namespace DotnetCrawler.Services
{
    public class CrawlerRequest
    {
        public string Base { get; set; } = string.Empty;
        public List<string> Threads { get; set; } = new List<string>();
        public string XfUser { get; set; } = string.Empty;
        public string XfSession { get; set; } = string.Empty;
        public string CfClearance { get; set; } = string.Empty;
    }

    public class CrawlerService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly StorageService _storageService;

        public CrawlerService(IServiceScopeFactory scopeFactory, StorageService storageService)
        {
            _scopeFactory = scopeFactory;
            _storageService = storageService;
        }

        public async Task StartCrawlAsync(CrawlerRequest request)
        {
            await ProcessCrawlAsync(request);
        }

        private async Task ProcessCrawlAsync(CrawlerRequest request)
        {
            Console.WriteLine("=== BẮT ĐẦU CRAWL BACKGROUND ===");

            var handler = new HttpClientHandler { UseCookies = false };
            using var client = new HttpClient(handler);
            var cookieHeader = $"xf_user={request.XfUser}; xf_session={request.XfSession}";
            if (!string.IsNullOrEmpty(request.CfClearance)) {
                cookieHeader += $"; cf_clearance={request.CfClearance}";
            }
            client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9,vi;q=0.8");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

            // Polly retry policy
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"[Polly] Lỗi mạng. Đang thử lại lần {retryCount} sau {timeSpan.TotalSeconds} giây...");
                });

            foreach (var threadUrl in request.Threads)
            {
                await CrawlThreadAsync(client, retryPolicy, request.Base, threadUrl);
            }

            Console.WriteLine("=== HOÀN TẤT CRAWL BACKGROUND ===");
        }

        private async Task CrawlThreadAsync(HttpClient client, IAsyncPolicy retryPolicy, string baseUrl, string threadUrl)
        {
            var url = baseUrl.TrimEnd('/') + "/" + threadUrl.TrimStart('/');
            var imagesDict = new Dictionary<string, string?>();
            var filesDict = new Dictionary<string, string>();

            while (!string.IsNullOrEmpty(url))
            {
                Console.WriteLine($"[Crawler] Đang tải trang: {url}");
                var response = await retryPolicy.ExecuteAsync(() => client.GetAsync(url));
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Lỗi tải trang: {response.StatusCode}. URL: {url}");
                }

                var html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Kiểm tra đăng nhập
                var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                if (titleNode != null && (titleNode.InnerText.Contains("Just a moment") || titleNode.InnerText.Contains("Cloudflare")))
                {
                    throw new Exception("CẢNH BÁO: Bị Cloudflare chặn! Vui lòng cập nhật Cookie xf_session và User-Agent mới.");
                }

                // Parse ảnh
                var anchorTags = doc.DocumentNode.SelectNodes("//a[contains(@class, 'js-lbImage')]");
                if (anchorTags != null)
                {
                    foreach (var a in anchorTags)
                    {
                        var href = a.GetAttributeValue("href", "");
                        if (!string.IsNullOrEmpty(href))
                        {
                            var imgUrl = href.StartsWith("http") ? href : new Uri(new Uri(baseUrl), href).ToString();
                            var mediaHref = a.GetAttributeValue("data-lb-sidebar-href", "");
                            string? mediaUrl = null;
                            if (!string.IsNullOrEmpty(mediaHref))
                            {
                                mediaUrl = new Uri(new Uri(baseUrl), mediaHref.Split('?')[0]).ToString();
                            }
                            imagesDict[imgUrl] = mediaUrl;
                        }
                    }
                }

                var imgTags = doc.DocumentNode.SelectNodes("//img[contains(@class, 'bbImage')]");
                if (imgTags != null)
                {
                    foreach (var img in imgTags)
                    {
                        var src = img.GetAttributeValue("src", "");
                        if (string.IsNullOrEmpty(src) || src.StartsWith("data:image")) 
                            src = img.GetAttributeValue("data-src", "");

                        if (!string.IsNullOrEmpty(src) && !src.Contains("smilies"))
                        {
                            var imgUrl = src.StartsWith("http") ? src : new Uri(new Uri(baseUrl), src).ToString();
                            if (!imagesDict.ContainsKey(imgUrl))
                                imagesDict[imgUrl] = null;
                        }
                    }
                }

                // Next page
                var nextBtn = doc.DocumentNode.SelectSingleNode("//a[contains(@class, 'pageNav-jump--next')]");
                if (nextBtn != null)
                {
                    var nextHref = nextBtn.GetAttributeValue("href", "");
                    url = new Uri(new Uri(baseUrl), nextHref).ToString();
                }
                else
                {
                    url = string.Empty;
                }
            }

            Console.WriteLine($"[Crawler] Đã tìm thấy {imagesDict.Count} ảnh trong thread {threadUrl}. Bắt đầu xử lý đa luồng...");

            // Logic extract subject / category / thread
            var parts = threadUrl.Trim('/').Split('/');
            var folderName = parts.Last().ToLower();
            var subjectCode = folderName.Contains("-") ? folderName.Split('-')[0] : "unknown";
            // Check Db
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Code == subjectCode);
            if (subject == null)
            {
                subject = new Subject { Code = subjectCode };
                db.Subjects.Add(subject);
                await db.SaveChangesAsync();
            }

            var courseThread = await db.Threads.FirstOrDefaultAsync(t => t.Title == folderName && t.SubjectId == subject.Id);
            if (courseThread == null)
            {
                // Guess category
                string category = "other";
                if (folderName.Contains("pe-") || folderName.Contains("-pe")) category = "pe";
                else if (folderName.Contains("fe-") || folderName.Contains("-fe")) category = "fe";

                courseThread = new CourseThread 
                { 
                    Title = folderName, 
                    SubjectId = subject.Id, 
                    Category = category,
                    Path = $"images/{subjectCode}/{category}/{folderName}"
                };
                db.Threads.Add(courseThread);
                await db.SaveChangesAsync();
            }

            var imageList = imagesDict.ToList();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 15 };

            await Parallel.ForEachAsync(imageList, parallelOptions, async (kvp, ct) =>
            {
                var imgLink = kvp.Key;
                var mediaUrl = kvp.Value;
                var idx = imageList.IndexOf(kvp);
                var imgName = $"img_{idx}.webp";
                var storageKey = $"images/{subjectCode}/{courseThread.Category}/{folderName}/{imgName}";

                try
                {
                    // 1. Tải ảnh & Stream qua Storage
                    using var imgRequest = new HttpRequestMessage(HttpMethod.Get, imgLink);
                    using var imgResponse = await client.SendAsync(imgRequest, HttpCompletionOption.ResponseHeadersRead, ct);
                    
                    if (imgResponse.IsSuccessStatusCode)
                    {
                        using var contentStream = await imgResponse.Content.ReadAsStreamAsync(ct);
                        var isUploaded = await _storageService.UploadImageStreamAsync(storageKey, contentStream, "image/webp");
                        if (isUploaded)
                        {
                            Console.WriteLine($"[Upload] Đã tải lên Storage: {storageKey}");
                        }
                    }

                    // 2. Parse comments (Vote)
                    string? bestAnswer = null;
                    var commentsExtracted = new List<Comment>();

                    if (!string.IsNullOrEmpty(mediaUrl))
                    {
                        var mRes = await retryPolicy.ExecuteAsync(() => client.GetAsync(mediaUrl));
                        var mHtml = await mRes.Content.ReadAsStringAsync();
                        var mDoc = new HtmlDocument();
                        mDoc.LoadHtml(mHtml);

                        var cNodes = mDoc.DocumentNode.SelectNodes("//div[contains(@class, 'comment-body')] | //article[contains(@class, 'message-body')]");
                        var ansCounts = new Dictionary<string, int> { {"A",0},{"B",0},{"C",0},{"D",0},{"E",0},{"F",0} };
                        var textDict = new Dictionary<string, Comment>();

                        if (cNodes != null)
                        {
                            foreach (var cNode in cNodes)
                            {
                                var text = cNode.InnerText.Trim().Replace("\n", " ").Replace("\t", "");
                                if (!string.IsNullOrEmpty(text) && !text.Contains("****") && !text.Contains("Mua gói thành viên"))
                                {
                                    var textLower = text.ToLower();
                                    if (!textDict.ContainsKey(textLower))
                                    {
                                        textDict[textLower] = new Comment { Text = text, Votes = 1 };
                                    }
                                    else
                                    {
                                        textDict[textLower].Votes++;
                                    }

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
                        }

                        commentsExtracted = textDict.Values.ToList();
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

                    // Lưu vào DB sử dụng context mới cho từng luồng vì DbContext không thread-safe
                    using var loopScope = _scopeFactory.CreateScope();
                    var loopDb = loopScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    
                    var existingQ = await loopDb.Questions.FirstOrDefaultAsync(q => q.CourseThreadId == courseThread.Id && q.ImageUrl == imgName);
                    if (existingQ == null)
                    {
                        var q = new Question
                        {
                            CourseThreadId = courseThread.Id,
                            ImageUrl = imgName,
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

                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Crawler] Lỗi tải/phân tích ảnh {imgName}: {e.Message}");
                }
            });

            Console.WriteLine($"[Crawler] Xong thread {threadUrl}");
        }
    }
}
