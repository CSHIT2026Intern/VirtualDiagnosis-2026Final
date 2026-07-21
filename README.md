
---

# VirtualDiagnosis

---

## 目錄

- [系統需求](#系統需求)
- [安裝步驟](#安裝步驟)
- [使用說明](#使用說明)
- [成績匯出](#成績匯出)
- [資料夾結構](#資料夾結構)

---

## 專案簡介

VirtualDiagnosis 提供虛擬病人問診情境，整合語音輸入、AI 病人回應、問診評分與自動生成回饋，協助醫學生或臨床人員進行模擬訓練。問診結束後可依實際作答結果，匯出正式格式的評分表 PDF 或 Excel/CSV。

---

## 系統需求

- Windows 10/11
- .NET 8.0
- Python 3.10
- [FFmpeg](https://ffmpeg.org/download.html)（需加入 PATH）
- Azure OpenAI API Key（或 OpenAI API Key）
- 語音輸入功能僅支援 Chrome / Edge 瀏覽器，且需在 `https://` 或 `localhost` 環境下才能取得麥克風權限

---

## 安裝步驟

### 1. 下載專案

```bash
git clone -b Final https://github.com/CSHIT2025Intern/VirtualDiagnosis.git
cd VirtualDiagnosis
```

### 2. 安裝 Python 依賴

建議使用 [Anaconda](https://www.anaconda.com/download) 虛擬環境：

```bash
conda create -n vdiagnosis python=3.10
conda activate vdiagnosis
```

#### 2-1. 安裝 torch（請依照硬體選擇 GPU 或 CPU 版本）

- **若有 NVIDIA GPU，建議安裝 GPU 版（速度較快）：**

  請參考 [PyTorch 官網安裝指令產生器](https://pytorch.org/get-started/locally/)  
  例如（以 CUDA 11.8 為例）：

  ```bash
  pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118
  ```

- **若僅有 CPU，請安裝 CPU 版：**

  ```bash
  pip install torch torchvision torchaudio
  ```

> 請勿同時安裝 GPU 與 CPU 版本，可能導致相依性衝突。

#### 2-2. 安裝其他依賴
```bash
pip install -r requirements_py.txt
```

### 3. 安裝 .NET 依賴

```bash
dotnet restore
```

### 4. 資料庫初始化

初次部署時，請先執行 `CreateVirtualPatDiagnosisDB.sql` 建立資料庫，再進行 EF Core migration（如有需要）。

### 5. 設定 FFmpeg

- 下載 [FFmpeg](https://ffmpeg.org/download.html)
- 將 ffmpeg.exe 所在資料夾加入系統環境變數 PATH

### 6. 設定 API 金鑰

請於 `appsettings.json` 設定 Azure OpenAI 或 OpenAI API Key 及 Endpoint。系統同時保留 `AzureOpenAI`（4o-mini）與 `AzureOpenAI_GPT5`（GPT-5-mini）兩組設定，實際使用哪一組由 `Controllers/GptController.cs` 建構子裡目前沒被註解的那一段決定，要切換模型可直接調整該處註解。


---

## 使用說明

1. 啟動服務：

   ```bash
   dotnet run
   ```

2. 開啟瀏覽器，進入 `http://localhost:xxxx`（依實際 port）

3. 點選註冊帳號，可選擇創建老師、學生和管理員身分（管理員尚無功能）

4. 依不同身分登入，即可使用對應功能

---

## 成績匯出

問診結束後的成績報告頁面，提供兩種匯出格式：

- **PDF**：依實際作答分數，自動產生跟紙本正式評分表相同排版的檔案，可直接列印或存檔
- **Excel / CSV**：純數據匯出，包含考生資訊與每一題得分，方便老師整理成績

---

## 資料夾結構

```
VirtualDiagnosis/
├── Controllers/                    # ASP.NET Core 控制器（後端 API 與頁面邏輯）
├── Models/                         # 資料模型（EF Core 實體、ViewModel 等）
├── Views/                          # Razor 頁面（前端 UI）
│   ├── Account/                    # 首頁
│   ├── ExamCase/                   # 教案管理頁
│   ├── Home/                       # 問診互動頁
│   └── Score/                      # 成績報告頁
├── Python/                         # 語音辨識與 TTS 相關 Python 程式
├── wwwroot/                        # 靜態資源（JS、CSS、圖片等）
│   ├── tts/                        # 系統生成的 TTS 音檔
│   ├── images/                     # 問診頁讀取的病人圖片
├── requirements_py.txt             # Python 依賴套件清單（不含 torch，請依 README 指示安裝）
├── appsettings.json                # ASP.NET Core 設定檔（API 金鑰、DB 連線等，勿 commit 真實金鑰）
├── 匯入教案範例.docx                # Word 匯入教案範例
├── CreateVirtualPatDiagnosisDB.sql # 建立資料庫 SQL 腳本
└── README.md                       # 專案說明文件
```

---

## Log 檔案說明

- 系統於每次 GPT 問答與評分時，會自動將完整 prompt（含對話記錄）、GPT 回應內容與各步驟耗時，寫入 log 檔案。
- Log 檔案預設儲存在：
  ```
  VirtualPatDiagnosis\bin\Debug\net8.0\GptLogs\
  ```

---
