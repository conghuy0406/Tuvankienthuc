using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainAi
{
    internal class MLData
    {
        public class TrainRow
        {
            public int MaSV { get; set; }      // int
            public int MaMH { get; set; }      // int
            public int MaKT { get; set; }      // int

            public float Mastery { get; set; }      // REAL -> float
            public float DoKho { get; set; }        // REAL -> float
            public float IsBasic { get; set; }      // REAL -> float
            public float RecencyDays { get; set; }  // REAL -> float

            public bool Label { get; set; }         // BIT -> bool
        }

        public class TrainPred
        {
            public bool PredictedLabel { get; set; }
            public float Probability { get; set; }
            public float Score { get; set; }
        }



    }
}
