using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using static alglib;
using static alglib.directdensesolvers;

namespace DiplomovaPracaLB
{
    public enum BASIS_FUNCTION
    {
        GAUSSIAN,
        MULTIQUADRATIC, //tato je vraj dobra na tereny
        INVERSE_QUADRATIC,
        INVERSE_MULTIQUADRATIC,
        THIN_PLATE  //tato je vraj dobra na tereny
    }

    internal class SplajnRadialBasis : Splajn
    {
        private double x_min, x_max, y_min, y_max;
        private int num_of_input_points;
        private int[] inputSize;

        public double shape_param;

        private BASIS_FUNCTION type_of_basis;

        //obe su velkosti cca (n*m)*(m*m-1)/2
        private double[,] RBFvalues; //hodnoty vzialenodstnej funkcie pre kazde 2 body - zavisi od vstupu a voľby bázy
        private double[] wieghts;

        public SplajnRadialBasis(ref TerrainData RefTerrain, int LOD, BASIS_FUNCTION type, double input_shape_param)
        {
            isRBF = true;
            type_of_basis = type;
            shape_param = input_shape_param;

            x_min = RefTerrain.GetMinMaxVal(false, 0);
            x_max = RefTerrain.GetMinMaxVal(true, 0);
            y_min = RefTerrain.GetMinMaxVal(false, 1);
            y_max = RefTerrain.GetMinMaxVal(true, 1);

            LoadDimensions(LOD, RefTerrain.GetSampleSize());

            Interpolate(ref RefTerrain);
        }

        protected override void LoadDimensions(int _Level_Of_Detail, int[] InputSize)
        {
            LOD = _Level_Of_Detail;
            m = (InputSize[0] - 1) * (LOD + 1) + 1;   //rovnake vzorkovanie ako originalne vsupne udaje, pretoze RBF su funkcie a rozdiel od ostatnych implementovanyh ploch
            n = (InputSize[1] - 1) * (LOD + 1) + 1;
            inputSize = new int[2] { InputSize[0], InputSize[1] };
            num_of_input_points = InputSize[0] * InputSize[1];
        }

        protected override Vector4[,] CreateInterpolationPoints(ref Vector4[,] Vstup)
        {
#if true
            //Garbage Colletion
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            //Stopwatch
            Stopwatch sw = new Stopwatch();
            sw.Start();

            SolveSystemToGetWeights(ref Vstup);

            sw.Stop();
            Console.WriteLine("      Elapsed SolveSystemToGetWeights = {0}", sw.Elapsed);
            sw.Reset();
#else
            SolveSystemToGetWeights(ref Vstup);
#endif




            //create points of interpolation with LOD:
            Vector4[,] IP = new Vector4[m, n];

            double x;
            double y;
            double z;
            double w = 1;

            for (int i = 0; i < Vstup.GetLength(0) - 1; i++)
            {
                for (int j = 0; j < Vstup.GetLength(1) - 1; j++)
                {
                    for (int lod_i = 0; lod_i <= LOD + 1; lod_i++)
                    {
                        for (int lod_j = 0; lod_j <= LOD + 1; lod_j++)
                        {
                            BilinearInterp(lod_i, lod_j, i, j, ref Vstup, out x, out y);

                            z = CalculateZ(x, y, Vstup);

                            IP[i * (LOD + 1) + lod_i, j * (LOD + 1) + lod_j] = new Vector4((float)x, (float)y, (float)z, (float)w);
                        }
                    }
                }
            }

            return IP;
        }


        private double CalculateZ(double x, double y, Vector4[,] Vstup)
        {
            double z = 0;
            double squared_dist;

            //nestasne zdlhave riesenie
            //prechadzam pole ako zoznam
            for (int i = 0; i < inputSize[0]; i++)
            {
                for (int j = 0; j < inputSize[1]; j++)
                {
                    squared_dist = SquaredEuclidianDist2D(x, y, Vstup[i, j].X, Vstup[i, j].Y);
                    if (squared_dist == 0)
                    {
                        //problem
                    }
                    int idx = i * inputSize[1] + j;
                    double tempw = wieghts[idx];
                    double temprbf = RBFunction(squared_dist);
                    z += tempw * temprbf;
                }
            }
            return z;
        }

        private double RBFunction(double squared_distance)
        {
            double rr = squared_distance;
            double cc = shape_param * shape_param;
            double rrcc = rr * cc;

            switch (type_of_basis)
            {
                case (BASIS_FUNCTION.GAUSSIAN):
                    {
                        return Math.Exp(-rrcc);
                    }
                case BASIS_FUNCTION.MULTIQUADRATIC:
                    {
                        return Math.Sqrt(1 + rrcc);
                    }
                case BASIS_FUNCTION.INVERSE_QUADRATIC:
                    {
                        return 1 / (1 + rrcc);
                    }
                case BASIS_FUNCTION.INVERSE_MULTIQUADRATIC:
                    {
                        return 1 / Math.Sqrt(1 + rrcc);
                    }
                case BASIS_FUNCTION.THIN_PLATE:  //todo veeelmi pomale
                    {
                        return (rr == 0) ? 0 : rr * Math.Log(Math.Sqrt(squared_distance));
                    }
                default:
                    return 0;
            }
        }

        private void ComputeRBFunctionValuesForDistances(ref Vector4[,] Vstup, ref double[,] MatrixOfValues)
        {
            MatrixOfValues = new double[num_of_input_points, num_of_input_points];
            //TODO prerobit, ak bude cas: najdi najblizsi bod, jeho indexy a okruhom prechadzaj body s najpodobnejsim indexom, ukonci "kruzenie" vtedy, ak prirastok bude mensi ako nejake epsilon

            for (int i = 0; i < inputSize[0]; i++)
            {
                for (int j = 0; j < inputSize[1]; j++)
                {
                    int idx1 = i * inputSize[1] + j;
                    for (int k = 0; k < inputSize[0]; k++)
                    {
                        for (int l = 0; l < inputSize[1]; l++)
                        {
                            int idx2 = k * inputSize[1] + l;
                            double dist = SquaredEuclidianDist2D(Vstup[i, j], Vstup[k, l]);
                            MatrixOfValues[idx1, idx2] = RBFunction(dist);
                        }
                    }
                }
            }
        }

        private void SolveSystemToGetWeights(ref Vector4[,] Vstup)
        {
            wieghts = new double[num_of_input_points];
            double[] inOutArray = new double[num_of_input_points];   //=inputsize0*inputsize1
            for (int i = 0; i < inputSize[0]; i++)
            {
                for (int j = 0; j < inputSize[1]; j++)
                {
                    inOutArray[i * inputSize[1] + j] = Vstup[i, j].Z;   //fill z values of point into the array
                }
            }

            //we must solve Aw = z, w=?

#if true
            //Garbage Colletion
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            //Stopwatch
            Stopwatch sw = new Stopwatch();
            sw.Start();

            ComputeRBFunctionValuesForDistances(ref Vstup, ref RBFvalues);  //velke PHI

            sw.Stop();
            Console.WriteLine("      Elapsed ComputeRBFunctionValuesForDistances = {0}", sw.Elapsed);
            sw.Reset();
#else
             ComputeRBFunctionValuesForDistances(ref Vstup, ref RBFvalues);  //velke PHI
#endif

#if true
            sw.Start();

            //inverse matrix
            matinvreport matinvreport = new matinvreport();

            sw.Stop();
            Console.WriteLine("      Elapsed matinvreport = {0}", sw.Elapsed);
            sw.Reset();
#else
             //inverse matrix
            matinvreport matinvreport = new matinvreport();
#endif

#if true
            sw.Start();

            int info = 0;
            alglib.densesolverlsreport densesolverlsreportt;
            rmatrixsolvels(RBFvalues, num_of_input_points, num_of_input_points, inOutArray, 0.0, out info, out densesolverlsreportt, out wieghts);

            sw.Stop();
            Console.WriteLine("      Elapsed rmatrixsolvels = {0}", sw.Elapsed);
            sw.Reset();
#else
           int info = 0;
            alglib.densesolverlsreport densesolverlsreportt;
            rmatrixsolvels(RBFvalues, num_of_input_points, num_of_input_points, inOutArray, 0.0, out info, out densesolverlsreportt, out wieghts);
#endif

        }
    }
}
