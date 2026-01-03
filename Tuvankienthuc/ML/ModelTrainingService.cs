using Microsoft.ML;
using Microsoft.ML.Data;

namespace Tuvankienthuc.ML
{
    public class ModelTrainingService
    {
        public (string modelPath, RegressionMetrics metrics)
            TrainAndEvaluate(string dataPath)
        {
            var ml = new MLContext(seed: 42);

            var data = ml.Data.LoadFromTextFile<LearningData>(
                dataPath,
                hasHeader: false,
                separatorChar: ',');

            var split = ml.Data.TrainTestSplit(data, testFraction: 0.2);

            var pipeline =
                ml.Transforms.Concatenate(
                        "Features",
                        nameof(LearningData.DoKho),
                        nameof(LearningData.SoKienThucTruoc),
                        nameof(LearningData.TrangThai)
                    )
                  .Append(
                        ml.Regression.Trainers.FastTree(
                            labelColumnName: nameof(LearningData.Score),
                            featureColumnName: "Features")
                   );

            var model = pipeline.Fit(split.TrainSet);

            var predictions = model.Transform(split.TestSet);

            var metrics = ml.Regression.Evaluate(
                predictions,
                labelColumnName: nameof(LearningData.Score));

            var modelPath = Path.Combine(
                "MLModels",
                $"knowledge_model_{DateTime.Now:yyyyMMddHHmmss}.zip");

            Directory.CreateDirectory("MLModels");
            ml.Model.Save(model, split.TrainSet.Schema, modelPath);

            return (modelPath, metrics);
        }
    }
}
