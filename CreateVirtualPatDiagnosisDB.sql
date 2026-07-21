CREATE DATABASE VirtualPatDiagnosis;
GO

USE VirtualPatDiagnosis;
GO

-- 使用者帳號資料表：記錄所有系統使用者（學生、教師、管理員）
CREATE TABLE UserAccount (
    Id INT PRIMARY KEY IDENTITY, -- 使用者唯一識別碼，自動遞增
    Username NVARCHAR(50) UNIQUE NOT NULL, -- 使用者帳號名稱，需唯一
    [Password] NVARCHAR(200) NOT NULL, -- 密碼
    DisplayName NVARCHAR(100), -- 顯示名稱（姓名或暱稱）
    [Role] NVARCHAR(20) NOT NULL CHECK ([Role] IN ('admin', 'teacher', 'student')) -- 角色類型
);

-- 題目組資料表：每組測驗主題對應一個病人情境與評分
CREATE TABLE ExamCase (
    Id INT PRIMARY KEY IDENTITY, -- 題組 ID
    Title NVARCHAR(200) NOT NULL, -- 題組標題
    [Description] NVARCHAR(MAX), -- 題組說明文字
    PassScore INT NOT NULL, -- 及格標準分數
    CreatedByUserId INT NOT NULL FOREIGN KEY REFERENCES UserAccount(Id), -- 建立此題組的教師帳號
    CreatedAt DATETIME DEFAULT GETDATE() -- 題組建立時間（預設為現在）
);

-- 病人檔案資料表：定義虛擬病患的病史資訊
CREATE TABLE PatientProfile (
    Id INT PRIMARY KEY IDENTITY, -- 病人檔案 ID
    ExamCaseId INT NOT NULL FOREIGN KEY REFERENCES ExamCase(Id), -- 所屬題組
    Complaint NVARCHAR(MAX), -- 主訴（病患來診的主要原因）
    CurrentHistory NVARCHAR(MAX), -- 現在病史（病況演變）
    PastHistory NVARCHAR(MAX), -- 過去病史（重大疾病、住院等）
    FamilyHistory NVARCHAR(MAX), -- 家族病史
    DrugHistory NVARCHAR(MAX), -- 藥物過敏或用藥史
    RestrictionRules NVARCHAR(MAX) -- 回答限制規則（如未被問到不可主動透露）
);

-- 評分檢核項目資料表：用於定義題組中的檢核面向
CREATE TABLE ChecklistItem (
    Id INT PRIMARY KEY IDENTITY, -- 檢核項目 ID
    ExamCaseId INT NOT NULL FOREIGN KEY REFERENCES ExamCase(Id), -- 所屬題組 ID
    [Name] NVARCHAR(200), -- 檢核項目名稱（如「嘔吐情況1」）
    [Description] NVARCHAR(MAX), -- 檢核項目說明
    MaxScore INT NOT NULL -- 此檢核項目最高可得分數
);

-- 關鍵詞資料表：定義學生須問到哪些詞語才算得分
CREATE TABLE KeyPhrase (
    Id INT PRIMARY KEY IDENTITY, -- 關鍵詞 ID
    ChecklistItemId INT NOT NULL FOREIGN KEY REFERENCES ChecklistItem(Id), -- 所屬檢核項目
    Phrase NVARCHAR(200) -- 關鍵詞文字（或語意）
);

-- 評分標準資料表：對每個 ChecklistItem 設定得分邏輯
CREATE TABLE ScoringRule (
    Id INT PRIMARY KEY IDENTITY, -- 評分規則 ID
    ChecklistItemId INT NOT NULL FOREIGN KEY REFERENCES ChecklistItem(Id), -- 所屬檢核項目
    [Level] NVARCHAR(50), -- 評分等級（完全做到/部分做到/未做到）
    [Description] NVARCHAR(200), -- 此等級對應的說明
    Score INT NOT NULL -- 對應的分數（0/1/2）
);

-- 對話記錄資料表：儲存模擬問診過程中，學生與病人的互動內容
CREATE TABLE ChatTurn (
    Id INT PRIMARY KEY IDENTITY, -- 對話紀錄 ID
    ExamCaseId INT NOT NULL FOREIGN KEY REFERENCES ExamCase(Id), -- 所屬題組
    UserRole NVARCHAR(20), -- 發話角色（student/patient）
    Content NVARCHAR(MAX), -- 對話文字內容
    AudioUrl NVARCHAR(300), -- 錄音檔案連結（如有）
    [Timestamp] DATETIME DEFAULT GETDATE() -- 發言時間
);

-- 學生整體作答紀錄：紀錄每一次學生完成的測驗與總成績
CREATE TABLE StudentResponse (
    Id INT PRIMARY KEY IDENTITY, -- 回答紀錄 ID
    ExamCaseId INT NOT NULL FOREIGN KEY REFERENCES ExamCase(Id), -- 所屬題組
    StudentUserId INT NOT NULL FOREIGN KEY REFERENCES UserAccount(Id), -- 學生帳號 ID
    StartTime DATETIME, -- 開始作答時間
    EndTime DATETIME, -- 結束作答時間
    FinalScore INT, -- 總分
    Summary NVARCHAR(MAX) -- 回饋摘要（如 AI 評語）
);

-- 每項檢核項目得分紀錄：詳細記錄學生在每項目的得分與配對結果
CREATE TABLE ChecklistScore (
    Id INT PRIMARY KEY IDENTITY, -- 評分紀錄 ID
    StudentResponseId INT NOT NULL FOREIGN KEY REFERENCES StudentResponse(Id), -- 所屬回答紀錄
    ChecklistItemId INT NOT NULL FOREIGN KEY REFERENCES ChecklistItem(Id), -- 所屬檢核項目
    MatchedPhrases NVARCHAR(MAX), -- 學生問到的關鍵詞（JSON格式或文字）
    [Level] NVARCHAR(50), -- 評級（完全/部分/未）
    Score INT, -- 分數
    Explanation NVARCHAR(MAX) -- 評分說明或 AI 解釋
);

GO

CREATE TABLE StudentRecord (
    Id INT PRIMARY KEY IDENTITY, -- 唯一識別碼
    StudentUserId INT NOT NULL FOREIGN KEY REFERENCES UserAccount(Id), -- 學生 ID
    ExamCaseId INT NOT NULL FOREIGN KEY REFERENCES ExamCase(Id), -- 題組 ID
    AttemptCount INT DEFAULT 0, -- 作答次數
    LastAttemptTime DATETIME, -- 最近作答時間
    HighestScore INT, -- 歷史最高分
    AverageScore FLOAT, -- 平均得分
    CONSTRAINT UQ_StudentExam UNIQUE (StudentUserId, ExamCaseId) -- 一位學生對同一題組唯一
);

ALTER TABLE StudentResponse
ADD StudentRecordId INT FOREIGN KEY REFERENCES StudentRecord(Id); -- 將單次作答對應至學生記錄

ALTER TABLE ExamCase
ADD TimeLimit INT NULL; -- 單位為「分鐘」

GO

ALTER TABLE PatientProfile
ADD [Name] NVARCHAR(50), Age INT;

GO

ALTER TABLE [PatientProfile]
ADD [QA] NVARCHAR(MAX) NULL;

GO

-- 將 KeyPhrases 和 ScoringRules 的屬性改回至 ChecklistItem 當中
ALTER TABLE ChecklistItem
ADD KeyPhrases NVARCHAR(MAX) NULL, -- 存多個關鍵詞: [{value":"嘔吐"},{"value":"頭暈"},{"value":"噁心"}]
    ScoringRules NVARCHAR(MAX) NULL; -- 存評分規則: [{"Level":"有問到兩項","Score":2},{"Level":"只問到一項","Score":1},{"Level":"完全沒有問到","Score":0}]

GO

-- 將原本的資料合併回 ChecklistItem
UPDATE ci
SET KeyPhrases = (
    SELECT k.Phrase
    FROM KeyPhrase k
    WHERE k.ChecklistItemId = ci.Id
    FOR JSON PATH
),
ScoringRules = (
    SELECT sr.Level, sr.Description, sr.Score
    FROM ScoringRule sr
    WHERE sr.ChecklistItemId = ci.Id
    FOR JSON AUTO
)
FROM ChecklistItem ci;

GO

UPDATE PatientProfile
SET QA = N'Q：媽媽您好!我是XXX醫師，是今天的小兒科值班醫師。 A：XXX醫師，您好。
- 主訴
Q：今天為什麼要來急診看病? A：小孩前天開始嘔吐，診所醫師建議到大醫院作進一步檢查。
Q：診所醫生有先做什麼處理嗎? A：醫生覺得可能是感冒造成，打了一針止吐針。
Q：有先服用什麼藥物治療過嗎? A：目前有服用診所開的感冒藥及腸胃藥物。
- 現在病史
Q：能不能告訴我什麼時候開始嘔吐? A：前天吃完晚餐後。
Q：能不能告訴我嘔吐的情況? A：前天吃完飯後吐了一次，昨天一天吐了七、八次，今天早上到現在四、五次，其他時間一直噁心。
Q：吐出來的東西是怎樣的? A：一開始吐出來的都是吃的東西。
Q：嘔吐物有咖啡色或鮮紅色的嗎? A：沒有。
Q：嘔吐物有黃綠色的嗎? A：今天早上有一點黃色的。
Q：這幾天排便的情況如何? 有拉肚子嗎? A：前兩天沒什麼特別，每天一次，但今天早上大便有點糊糊的。
Q：便便是怎樣的?有黑便或紅色的血嗎? A：沒有。
Q：這幾天小便的情況如何? 顏色怎樣? A：前兩天沒什麼特別，但今天早上到現在只有一次，而且量很少、顏色很深。
Q：這幾天小孩有肚子不舒服嗎? A：應該有吧!
Q：痛在哪裡? A：這兩天小美一直雙手摸著肚子，我想整個肚子都不舒服吧!
Q：會一陣一陣哭鬧嗎? A：偶而會哭鬧不安。
Q：她最近有其他不舒服嗎? A：醫師您指的是?
Q：例如咳嗽、流鼻水? A：有。
Q：這幾天小孩有發燒嗎? A：算有吧!
Q：什麼時候發燒?體溫多少? A：一星期前感冒時體溫有點高，約38度，這幾天已經沒發燒。
Q：這幾天小孩有吃到不潔食物嗎? 或吃到什麼以前沒吃過的食物? A：應該沒有!小美一直都在家裏，應該不會吃到什麼特別的食物。
Q：最近小孩有沒有可能跌倒撞到肚子? A：最近小孩剛學走路，常常跌跌撞撞，到處碰撞。
Q：什麼時候?撞到哪裡? A：沒有注意有沒有撞到肚子!
Q：你剛剛說小美最近學走路常跌倒，那小孩有沒有可能撞到頭部?什麼時候? A：前兩天有碰到沙發椅背，但看來沒有什麼不舒服和其他特別的症狀。
- 基本資料
Q：小孩多大? A：今年1歲2個月
- 出生史
Q：小孩在哪裡出生? A：貴院。
- 過去病史
Q：以前有沒有什麼疾病? A：小美從出生後身體都還不錯，沒有特別疾病。
Q：以前有沒有手術過? A：有，六個月大時因右側腹股溝疝氣接受手術。
Q：以前有沒有住院過? A：除了疝氣手術住院外，其他沒有住院過。
- 藥物史
Q：有對什麼藥物過敏嗎? A：沒有。
Q：目前服用藥物嗎? A：目前有服用診所開的感冒及腸胃藥物。
Q：有沒有長期服用什麼藥? A：沒有。
- 過敏史
Q：有對什麼過敏過嗎? A：沒有
- 家族史
Q：家裏成員有沒有什麼疾病嗎?最近有沒有誰有腸胃不舒服? A：家人健康狀況都不錯，最近沒有人有腸胃不舒服。
- 寵物飼養
Q：家裡有沒有養寵物? A：沒有。
- 旅遊史
Q：最近有沒有出門遊玩過或出國? A：沒有。
'
WHERE ExamCaseId = 3;

GO

-- 時間不足以完成學生端，先以此臨時資料表完成結果呈現之實作
CREATE TABLE ExamResults (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ExamCaseId INT NOT NULL,
    SubmittedAt DATETIME2 NOT NULL,
    TotalScore INT NOT NULL,
    GptResultJson NVARCHAR(MAX) NULL
);

GO

ALTER TABLE ExamResults
ADD StartTime DATETIME2 NOT NULL DEFAULT GETDATE();

GO

-- 建立索引，加快 SQL 查詢速度
CREATE INDEX IX_PatientProfile_ExamCaseId ON PatientProfile(ExamCaseId);

GO