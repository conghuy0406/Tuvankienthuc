using static Tuvankienthuc.ML.MLTypes;
using Microsoft.Extensions.ML;
namespace Tuvankienthuc.ML
{
    public class RealMLScore : IMLScoreService
    {
        private readonly PredictionEnginePool<TrainRow, TrainPred> _pool;

        public RealMLScore(PredictionEnginePool<TrainRow, TrainPred> pool)
        {
            _pool = pool;
        }

        public float Score(TrainRow row)
        {
            var pred = _pool.Predict("DexuatModel", row);
            return pred.Probability; // trả về xác suất 0..1
        }
    }
}
