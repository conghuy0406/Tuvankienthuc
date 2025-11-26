namespace Tuvankienthuc.ML
{
    public class MLTypes
    {
        public class TrainRow
        {
            public int MaSV { get; set; }
            public int MaMH { get; set; }
            public int MaKT { get; set; }

            public float Mastery { get; set; }
            public float DoKho { get; set; }
            public float IsBasic { get; set; }
            public float RecencyDays { get; set; }

            public bool Label { get; set; } // chỉ dùng khi train, WebApp không cần gán
        }

        public class TrainPred
        {
            public bool PredictedLabel { get; set; }
            public float Probability { get; set; }
            public float Score { get; set; }
        }

        public interface IMLScoreService
        {
            float Score(TrainRow row);
        }
    }
}
