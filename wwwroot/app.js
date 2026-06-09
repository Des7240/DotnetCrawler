/**
 * DOMElements holds references to all necessary DOM elements.
 */
const DOMElements = {
  // Selectors
  subjectSelect: document.getElementById('select-subject'),
  threadSelect: document.getElementById('select-thread'),
  loadBtn: document.getElementById('btn-load'),
  
  // View Modes
  modeSelector: document.getElementById('mode-selector'),
  btnReviewMode: document.getElementById('btn-review-mode'),
  btnQuizMode: document.getElementById('btn-quiz-mode'),
  
  // Panels
  mainContent: document.getElementById('main-content'),
  quizPanel: document.getElementById('quiz-panel'),
  reviewPanel: document.getElementById('review-panel'),
  filesManager: document.getElementById('files-manager'),
  filesList: document.getElementById('files-list'),
  
  // Navigation
  navBar: document.getElementById('nav-bar'),
  prevBtn: document.getElementById('btn-prev'),
  nextBtn: document.getElementById('btn-next'),
  shuffleBtn: document.getElementById('btn-shuffle'),
  questionCounter: document.getElementById('question-counter'),
  
  // Content
  questionImage: document.getElementById('question-image'),
  zoomLevel: document.getElementById('zoom-level'),
  zoomIn: document.getElementById('zoom-in'),
  zoomOut: document.getElementById('zoom-out'),
  zoomReset: document.getElementById('zoom-reset'),
  
  // Quiz
  optBtns: document.querySelectorAll('.opt-btn'),
  quizFeedback: document.getElementById('quiz-feedback'),
  
  // Review (Comments & Gemini)
  commentsList: document.getElementById('comments-list'),
  btnGeminiSettings: document.getElementById('btn-gemini-settings'),
  btnAskGemini: document.getElementById('btn-ask-gemini'),
  geminiSetup: document.getElementById('gemini-setup'),
  geminiApiKey: document.getElementById('gemini-api-key'),
  geminiModel: document.getElementById('gemini-model'),
  btnSaveGemini: document.getElementById('btn-save-gemini'),
  geminiResponse: document.getElementById('gemini-response')
};

// State
let state = {
  questions: [],
  currentIndex: 0,
  zoom: 100,
  mode: 'review', // 'review' or 'quiz'
  geminiKey: localStorage.getItem('gemini_api_key') || '',
  geminiModel: localStorage.getItem('gemini_model') || 'gemini-2.0-flash'
};

/**
 * Initializes the application.
 */
async function initApp() {
  try {
    setupEventListeners();
    await loadSubjects();
    
    // Init Gemini settings UI
    if (state.geminiKey) {
      DOMElements.geminiApiKey.value = state.geminiKey;
      DOMElements.geminiModel.value = state.geminiModel;
    }
  } catch (error) {
    console.error('Error initializing app:', error);
    alert('Không thể khởi tạo ứng dụng. Vui lòng kiểm tra kết nối.');
  }
}

/**
 * Sets up all DOM event listeners.
 */
function setupEventListeners() {
  DOMElements.subjectSelect.addEventListener('change', handleSubjectChange);
  DOMElements.threadSelect.addEventListener('change', handleThreadChange);
  DOMElements.loadBtn.addEventListener('click', loadThreadData);
  
  DOMElements.prevBtn.addEventListener('click', () => navigateQuestion(-1));
  DOMElements.nextBtn.addEventListener('click', () => navigateQuestion(1));
  DOMElements.shuffleBtn.addEventListener('click', shuffleQuestions);
  
  DOMElements.btnReviewMode.addEventListener('click', () => switchMode('review'));
  DOMElements.btnQuizMode.addEventListener('click', () => switchMode('quiz'));
  
  DOMElements.zoomIn.addEventListener('click', () => changeZoom(10));
  DOMElements.zoomOut.addEventListener('click', () => changeZoom(-10));
  DOMElements.zoomReset.addEventListener('click', () => changeZoom(0, true));
  
  DOMElements.optBtns.forEach(btn => {
    btn.addEventListener('click', (e) => handleQuizAnswer(e.target.dataset.val));
  });
  
  DOMElements.btnGeminiSettings.addEventListener('click', () => {
    DOMElements.geminiSetup.classList.toggle('hidden');
  });
  
  DOMElements.btnSaveGemini.addEventListener('click', saveGeminiSettings);
  DOMElements.btnAskGemini.addEventListener('click', handleAskGemini);
}

/**
 * Loads subjects from API and populates the select.
 */
async function loadSubjects() {
  try {
    const response = await fetch('/api/course/subjects');
    const subjects = await response.json();
    
    DOMElements.subjectSelect.innerHTML = '<option value="">-- Chọn Môn --</option>';
    subjects.forEach(s => {
      const option = document.createElement('option');
      option.value = s.id;
      option.textContent = s.code.toUpperCase();
      DOMElements.subjectSelect.appendChild(option);
    });
  } catch (error) {
    console.error('Failed to load subjects:', error);
    throw error;
  }
}

/**
 * Handles subject selection change.
 * @param {Event} event - The change event.
 */
async function handleSubjectChange(event) {
  const subjectId = event.target.value;
  DOMElements.threadSelect.innerHTML = '<option value="">-- Chọn Đề --</option>';
  DOMElements.threadSelect.disabled = true;
  
  if (!subjectId) return;
  
  try {
    const response = await fetch(`/api/course/threads?subjectId=${subjectId}`);
    const threads = await response.json();
    
    threads.forEach(t => {
      const option = document.createElement('option');
      option.value = t.id;
      option.textContent = `[${t.category.toUpperCase()}] ${t.title || t.path}`;
      DOMElements.threadSelect.appendChild(option);
    });
    
    DOMElements.threadSelect.disabled = false;
  } catch (error) {
    console.error('Failed to load threads:', error);
  }
}

/**
 * Handles thread selection change.
 */
function handleThreadChange() {
  DOMElements.loadBtn.disabled = !DOMElements.threadSelect.value;
}

/**
 * Loads questions and files for the selected thread.
 */
async function loadThreadData() {
  const threadId = DOMElements.threadSelect.value;
  if (!threadId) return;
  
  try {
    DOMElements.loadBtn.textContent = 'Đang tải...';
    DOMElements.loadBtn.disabled = true;
    
    // Fetch both questions and files concurrently
    const [questionsRes, filesRes] = await Promise.all([
      fetch(`/api/course/questions?threadId=${threadId}`),
      fetch(`/api/course/files?threadId=${threadId}`)
    ]);
    
    state.questions = await questionsRes.json();
    const files = await filesRes.json();
    
    state.currentIndex = 0;
    renderFiles(files);
    
    if (state.questions.length > 0) {
      DOMElements.modeSelector.classList.remove('hidden');
      DOMElements.navBar.classList.remove('hidden');
      DOMElements.mainContent.classList.remove('hidden');
      renderQuestion();
    } else {
      DOMElements.questionImage.src = '';
      alert('Không có câu hỏi nào trong đề này.');
    }
  } catch (error) {
    console.error('Failed to load thread data:', error);
    alert('Lỗi tải dữ liệu đề thi.');
  } finally {
    DOMElements.loadBtn.textContent = 'TẢI ĐỀ LÊN';
    DOMElements.loadBtn.disabled = false;
  }
}

/**
 * Renders attached files if any.
 * @param {Array} files - Array of file objects.
 */
function renderFiles(files) {
  if (files && files.length > 0) {
    DOMElements.filesManager.classList.remove('hidden');
    DOMElements.filesList.innerHTML = files.map(f => 
      `<li><a href="${f.url}" target="_blank">${f.name || 'File đính kèm'}</a></li>`
    ).join('');
  } else {
    DOMElements.filesManager.classList.add('hidden');
    DOMElements.filesList.innerHTML = '';
  }
}

/**
 * Displays the current question.
 */
function renderQuestion() {
  if (state.questions.length === 0) return;
  
  const q = state.questions[state.currentIndex];
  DOMElements.questionCounter.textContent = `Câu ${state.currentIndex + 1} / ${state.questions.length}`;
  DOMElements.questionImage.src = q.image || '';
  
  // Reset Zoom
  changeZoom(0, true);
  
  // Update UI for the current mode
  if (state.mode === 'quiz') {
    resetQuizUI();
  } else {
    renderReviewComments(q);
  }
  
  // Handle Gemini Response reset
  DOMElements.geminiResponse.classList.add('hidden');
  DOMElements.geminiResponse.innerHTML = '';
  
  updateNavButtons();
}

/**
 * Navigates to the previous or next question.
 * @param {number} direction - Direction to navigate (-1 or 1).
 */
function navigateQuestion(direction) {
  const newIndex = state.currentIndex + direction;
  if (newIndex >= 0 && newIndex < state.questions.length) {
    state.currentIndex = newIndex;
    renderQuestion();
  }
}

/**
 * Shuffles the questions array.
 */
function shuffleQuestions() {
  for (let i = state.questions.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [state.questions[i], state.questions[j]] = [state.questions[j], state.questions[i]];
  }
  state.currentIndex = 0;
  renderQuestion();
}

/**
 * Changes zoom level of the question image.
 * @param {number} amount - Amount to zoom by.
 * @param {boolean} reset - Whether to reset zoom to 100%.
 */
function changeZoom(amount, reset = false) {
  if (reset) {
    state.zoom = 100;
  } else {
    state.zoom = Math.max(50, Math.min(300, state.zoom + amount));
  }
  DOMElements.zoomLevel.textContent = `${state.zoom}%`;
  DOMElements.questionImage.style.transform = `scale(${state.zoom / 100})`;
  DOMElements.questionImage.style.transformOrigin = 'top center';
}

/**
 * Switches between 'review' and 'quiz' mode.
 * @param {string} mode - The mode to switch to.
 */
function switchMode(mode) {
  state.mode = mode;
  if (mode === 'quiz') {
    DOMElements.btnQuizMode.classList.add('active');
    DOMElements.btnReviewMode.classList.remove('active');
    DOMElements.quizPanel.classList.remove('hidden');
    DOMElements.reviewPanel.classList.add('hidden');
    resetQuizUI();
  } else {
    DOMElements.btnReviewMode.classList.add('active');
    DOMElements.btnQuizMode.classList.remove('active');
    DOMElements.reviewPanel.classList.remove('hidden');
    DOMElements.quizPanel.classList.add('hidden');
    renderReviewComments(state.questions[state.currentIndex]);
  }
}

/**
 * Resets the Quiz UI (buttons, feedback).
 */
function resetQuizUI() {
  DOMElements.optBtns.forEach(btn => {
    btn.classList.remove('correct', 'wrong');
    btn.disabled = false;
  });
  DOMElements.quizFeedback.classList.add('hidden');
  DOMElements.quizFeedback.className = '';
  DOMElements.quizFeedback.innerHTML = '';
}

/**
 * Handles answering a question in quiz mode.
 * @param {string} selectedOption - The chosen option ('A', 'B', ...).
 */
function handleQuizAnswer(selectedOption) {
  const q = state.questions[state.currentIndex];
  const bestAnswer = q.best_answer?.toUpperCase();
  
  DOMElements.optBtns.forEach(btn => btn.disabled = true);
  DOMElements.quizFeedback.classList.remove('hidden');
  
  if (!bestAnswer) {
    DOMElements.quizFeedback.textContent = 'Câu hỏi này chưa có đáp án được xác nhận.';
    DOMElements.quizFeedback.style.color = '#e67e22';
    return;
  }
  
  if (selectedOption === bestAnswer) {
    DOMElements.quizFeedback.innerHTML = `✅ Chính xác! Đáp án là ${bestAnswer}`;
    DOMElements.quizFeedback.style.color = '#27ae60';
  } else {
    DOMElements.quizFeedback.innerHTML = `❌ Sai rồi! Đáp án đúng là ${bestAnswer}`;
    DOMElements.quizFeedback.style.color = '#e74c3c';
  }
  
  DOMElements.optBtns.forEach(btn => {
    if (btn.dataset.val === bestAnswer) {
      btn.classList.add('correct');
    } else if (btn.dataset.val === selectedOption && selectedOption !== bestAnswer) {
      btn.classList.add('wrong');
    }
  });
}

/**
 * Renders comments for the review mode.
 * @param {Object} q - The question object.
 */
function renderReviewComments(q) {
  if (!q || !q.comments || q.comments.length === 0) {
    DOMElements.commentsList.innerHTML = '<p style="color:#7f8c8d; font-style:italic;">Chưa có bình luận hoặc gợi ý nào cho câu này.</p>';
    return;
  }
  
  const sortedComments = [...q.comments].sort((a, b) => b.count - a.count);
  
  DOMElements.commentsList.innerHTML = sortedComments.map(c => {
    const isBest = q.best_answer && c.text.toUpperCase().includes(q.best_answer.toUpperCase());
    const bgClass = isBest ? 'background: #e8f8f5; border-left: 4px solid #2ecc71;' : '';
    return `
      <div class="comment-item" style="${bgClass}">
        <div style="font-weight:bold; color:#2c3e50; margin-bottom:4px;">
          ${isBest ? '⭐ Đáp án nổi bật' : 'Bình luận'}
          <span class="comment-count">(+${c.count} votes)</span>
        </div>
        <div style="color:#34495e;">${c.text}</div>
      </div>
    `;
  }).join('');
}

/**
 * Saves Gemini API Settings.
 */
function saveGeminiSettings() {
  const key = DOMElements.geminiApiKey.value.trim();
  const model = DOMElements.geminiModel.value;
  
  if (key) {
    localStorage.setItem('gemini_api_key', key);
    localStorage.setItem('gemini_model', model);
    state.geminiKey = key;
    state.geminiModel = model;
    DOMElements.geminiSetup.classList.add('hidden');
    alert('Đã lưu cấu hình Gemini API!');
  } else {
    alert('Vui lòng nhập API Key hợp lệ.');
  }
}

/**
 * Handles asking Gemini for explanation.
 */
async function handleAskGemini() {
  if (!state.geminiKey) {
    DOMElements.geminiSetup.classList.remove('hidden');
    alert('Vui lòng nhập API Key để sử dụng.');
    return;
  }
  
  const q = state.questions[state.currentIndex];
  if (!q || !q.image) return;
  
  // If we already have gemini_answer from backend, display it
  if (q.gemini_answer) {
    displayGeminiResponse(q.gemini_answer);
    return;
  }
  
  DOMElements.btnAskGemini.disabled = true;
  DOMElements.btnAskGemini.textContent = 'Đang phân tích...';
  
  try {
    const imageUrl = q.image;
    // Extract base64 if it's base64, otherwise we can't easily pass URL to Gemini API from client due to CORS.
    // Assuming backend returns absolute URL. We might need a backend proxy.
    // For this client-side implementation, we'll try to fetch the image and convert to base64.
    
    const imgRes = await fetch(imageUrl);
    const imgBlob = await imgRes.blob();
    const reader = new FileReader();
    
    reader.onloadend = async () => {
      const base64data = reader.result.split(',')[1];
      const mimeType = imgBlob.type;
      
      const payload = {
        contents: [{
          parts: [
            { text: "Giải thích câu hỏi trắc nghiệm này chi tiết và đưa ra đáp án cuối cùng. Phân tích từng lựa chọn tại sao đúng, tại sao sai." },
            { inlineData: { mimeType, data: base64data } }
          ]
        }]
      };
      
      const apiUrl = `https://generativelanguage.googleapis.com/v1beta/models/${state.geminiModel}:generateContent?key=${state.geminiKey}`;
      
      const gRes = await fetch(apiUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      
      const gData = await gRes.json();
      if (gData.error) throw new Error(gData.error.message);
      
      const text = gData.candidates?.[0]?.content?.parts?.[0]?.text || 'Không nhận được phản hồi.';
      
      // Update local object to prevent re-fetching
      q.gemini_answer = text;
      displayGeminiResponse(text);
      
      // TODO: Optionally, send a POST request to backend to save gemini_answer to database.
    };
    reader.readAsDataURL(imgBlob);
    
  } catch (error) {
    console.error('Gemini error:', error);
    displayGeminiResponse(`Lỗi: ${error.message}`);
  } finally {
    DOMElements.btnAskGemini.disabled = false;
    DOMElements.btnAskGemini.textContent = '🤖 Hỏi Gemini';
  }
}

/**
 * Displays the Gemini response nicely formatted.
 * @param {string} text - The response text (markdown).
 */
function displayGeminiResponse(text) {
  DOMElements.geminiResponse.classList.remove('hidden');
  
  // Simple markdown parser (bold, list, newlines)
  let html = text
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.*?)\*/g, '<em>$1</em>')
    .replace(/\n\n/g, '</p><p>')
    .replace(/\n/g, '<br>');
    
  DOMElements.geminiResponse.innerHTML = `<p>${html}</p>`;
}

/**
 * Updates Next/Prev button states.
 */
function updateNavButtons() {
  DOMElements.prevBtn.disabled = state.currentIndex === 0;
  DOMElements.nextBtn.disabled = state.currentIndex === state.questions.length - 1;
}

// Start Application
document.addEventListener('DOMContentLoaded', initApp);
