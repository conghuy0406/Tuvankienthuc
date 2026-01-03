//using Microsoft.ML;
//namespace Tuvankienthuc.ML
//{
//    public class ModelLoader
//    {
//        private readonly MLContext _ml = new();
//        private ITransformer? _model;

//        public void Load(string path)
//        {
//            _model = _ml.Model.Load(path, out _);
//        }

//        public LearningPrediction Predict(LearningData input)
//        {
//            var engine = _ml.Model.CreatePredictionEngine<LearningData, LearningPrediction>(_model!);
//            return engine.Predict(input);
//        }
//    }


//}
