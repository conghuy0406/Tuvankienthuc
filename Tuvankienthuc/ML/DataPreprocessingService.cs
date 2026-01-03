using Microsoft.ML;

namespace Tuvankienthuc.ML
{
    public class DataPreprocessingService
    {
        public IDataView LoadData(MLContext ml, string csvPath)
        {
            return ml.Data.LoadFromTextFile<LearningData>(
                path: csvPath,
                hasHeader: false,
                separatorChar: ',');
        }
    }
}
