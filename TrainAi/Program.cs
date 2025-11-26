using System;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.ML;
using Microsoft.ML.Data;
using TrainAi;

class Program
{
    static void Main(string[] args)
    {
        var ml = new MLContext(seed: 42);

        // 1. Load data từ view vw_ML_Train
        var loader = ml.Data.CreateDatabaseLoader<MLData.TrainRow>();

        // ⚠️ Connection string
        var conn = "Server=DESKTOP-LBPEUVQ;Database=Tuvankienthuc;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        var sql = "SELECT * FROM vw_ML_Train";

        var data = loader.Load(new DatabaseSource(SqlClientFactory.Instance, conn, sql));

        // 2. Đếm số bản ghi
        var rows = ml.Data.CreateEnumerable<MLData.TrainRow>(data, reuseRowObject: false).Count();
        Console.WriteLine($"📊 Số bản ghi trong dataset: {rows}");

        if (rows == 0)
        {
            Console.WriteLine("⚠ Không có dữ liệu để train. Hãy kiểm tra lại view vw_ML_Train.");
            return;
        }

        // 3. Pipeline
        var pipeline = ml.Transforms.Concatenate("Features",
                                nameof(MLData.TrainRow.Mastery),
                                nameof(MLData.TrainRow.DoKho),
                                nameof(MLData.TrainRow.IsBasic),
                                nameof(MLData.TrainRow.RecencyDays))
                      .Append(ml.Transforms.NormalizeMinMax("Features"))
                      .Append(ml.BinaryClassification.Trainers.LightGbm(
                          labelColumnName: nameof(MLData.TrainRow.Label),
                          featureColumnName: "Features"));

        // 4. Train
        ITransformer model;
        if (rows <= 50)
        {
            Console.WriteLine("⚠ Dữ liệu quá ít → Train toàn bộ dataset, bỏ qua test split.");
            model = pipeline.Fit(data);
        }
        else
        {
            Console.WriteLine("✅ Đang chia train/test (80/20)...");
            var split = ml.Data.TrainTestSplit(data, testFraction: 0.2, seed: 42);

            model = pipeline.Fit(split.TrainSet);

            // 5. Evaluate
            var preds = model.Transform(split.TestSet);
            var metrics = ml.BinaryClassification.Evaluate(preds, labelColumnName: nameof(MLData.TrainRow.Label));

            Console.WriteLine($"📈 AUC = {metrics.AreaUnderRocCurve:F3}");
            Console.WriteLine($"📈 F1  = {metrics.F1Score:F3}");
            Console.WriteLine($"📈 Acc = {metrics.Accuracy:F3}");
        }

        // 6. Save model vào WebApp/MLModels
        var savePath = Path.Combine("..", "Tuvankienthuc", "Tuvankienthuc", "MLModels", "model_dx.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
        ml.Model.Save(model, data.Schema, savePath);

        Console.WriteLine($"✅ Model saved to {Path.GetFullPath(savePath)}");
    }
}
