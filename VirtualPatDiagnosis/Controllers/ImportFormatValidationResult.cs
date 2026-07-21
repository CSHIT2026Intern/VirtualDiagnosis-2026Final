using System.Text.RegularExpressions;

namespace VirtualPatDiagnosis.Helpers
{
    /// <summary>
    /// 驗證 Word 教案是否仍符合系統可解析的匯入範本。
    /// </summary>
    public sealed class ImportFormatValidationResult
    {
        public List<string> Errors { get; } = new();
        public bool IsValid => Errors.Count == 0;

        public static ImportFormatValidationResult Validate(string? documentText)
        {
            var result = new ImportFormatValidationResult();

            if (string.IsNullOrWhiteSpace(documentText))
            {
                result.Errors.Add("文件沒有可讀取的文字內容。");
                return result;
            }

            // 欄位名稱有別名時仍可通過；只在系統無法辨識整個必要區塊時才拒絕。
            var caseInfo = DocumentParserHelper.ParseExamCaseInfo(documentText);
            if (string.IsNullOrWhiteSpace(caseInfo.Title) && !caseInfo.TimeLimit.HasValue && !caseInfo.PassScore.HasValue)
                result.Errors.Add("無法辨識教案基本資料（名稱、時間或及格分數）。");

            var patient = DocumentParserHelper.ParsePatientProfile(documentText);
            if (string.IsNullOrWhiteSpace(patient.Name) && string.IsNullOrWhiteSpace(patient.Complaint) && string.IsNullOrWhiteSpace(patient.CurrentHistory))
                result.Errors.Add("無法辨識病人資料（姓名、主訴或現病史）。");

            var checklists = DocumentParserHelper.ParseChecklists(documentText);
            if (checklists.Count == 0)
                result.Errors.Add("無法辨識任何評分檢核項目。請確認有評分項目標題、項目名稱與編號。");

            return result;
        }

    }
}       