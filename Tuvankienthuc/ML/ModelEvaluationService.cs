using Microsoft.ML;
using Microsoft.ML.Data;

namespace Tuvankienthuc.ML
{
    public class ModelEvaluationService
    {
        public (double acc, double auc, double f1) GetMetrics(CalibratedBinaryClassificationMetrics m)
            => (m.Accuracy, m.AreaUnderRocCurve, m.F1Score);
    }
}
