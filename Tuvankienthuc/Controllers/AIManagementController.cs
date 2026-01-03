using Microsoft.AspNetCore.Mvc;
using Tuvankienthuc.Filters;
using Tuvankienthuc.ML;

namespace Tuvankienthuc.Controllers
{
    [RoleAuthorize("Admin")]
    public class AIManagementController : Controller
    {
        private readonly DataCollectorService _collector;
        private readonly ModelTrainingService _trainer;

        public AIManagementController(DataCollectorService collector, ModelTrainingService trainer)
        {
            _collector = collector;
            _trainer = trainer;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult TrainModel() => View();



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrainModel(int? maMH)
        {
            var dataPath = await _collector.ExportTrainingDataAsync(maMH);

            var (modelPath, metrics) =
                _trainer.TrainAndEvaluate(dataPath);

            ViewBag.Result = new TrainResultVm
            {
                DataPath = dataPath,
                ModelPath = modelPath,
                R2 = metrics.RSquared,
                RMSE = metrics.RootMeanSquaredError,
                MAE = metrics.MeanAbsoluteError,
                Note = "Regression model dự đoán độ phù hợp kiến thức"
            };

            return View();
        }



    }
}
