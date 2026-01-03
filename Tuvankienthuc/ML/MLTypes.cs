using Microsoft.ML.Data;

namespace Tuvankienthuc.ML
{
    public class LearningData
    {

        [LoadColumn(0)]
        public float Score { get; set; }

        [LoadColumn(1)]
        public float DoKho { get; set; }

        [LoadColumn(2)]
        public float SoKienThucTruoc { get; set; }

        [LoadColumn(3)]
        public float TrangThai { get; set; }
    }
    public class TrainResultVm
    {
        public string ModelPath { get; set; } = "";
        public string DataPath { get; set; } = "";

        // Regression metrics
        public double R2 { get; set; }
        public double RMSE { get; set; }
        public double MAE { get; set; }

        public string Note { get; set; } = "";
    }

    public class ModelOutput
    {
        [ColumnName("Score")]
        public float PredictedScore { get; set; }
    }
}
