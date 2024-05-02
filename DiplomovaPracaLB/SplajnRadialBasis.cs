using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using static alglib;

namespace DiplomovaPracaLB
{
    public enum BASIS_FUNCTION
    {
        GAUSSIAN,
        MULTIQUADRATIC, //tato je vraj dobra na tereny
        INVERSE_QUADRATIC,
        INVERSE_MULTIQUADRATIC,
        POLYHARMONIC,
        THIN_PLATE  //tato je vraj dobra na tereny
    }

    internal class SplajnRadialBasis : Splajn
    {
        private double x_min, x_max, y_min, y_max;
        private int num_of_input_points;
        private int[] inputSize;

        public double shape_param;
        private double Hardy_param;
        private double Fasshauer_param;
        
        private BASIS_FUNCTION type_of_basis;
        
        //obe su velkosti cca (n*m)*(m*m-1)/2
        //private double[,] distances; // vzdialenosti pre kazde 2 body budu rovnake pre rovnaky vstup - zavisi od vstupu
        private double[,] RBFvalues; //hodnoty vzialenodstnej funkcie pre kazde 2 body - zavisi od vstupu a voľby bázy
        private double[] wieghts;

        public SplajnRadialBasis(ref TerrainData RefTerrain, int LOD, BASIS_FUNCTION type, double input_shape_param)
        {
            
            isRBF = true;
            LoadDimensions(LOD, RefTerrain.GetSampleSize());

            x_min = RefTerrain.GetMinMaxVal(false, 0);
            x_max = RefTerrain.GetMinMaxVal(true, 0);
            y_min = RefTerrain.GetMinMaxVal(false, 1);
            y_max = RefTerrain.GetMinMaxVal(true, 1);

            type_of_basis = type;
            shape_param = input_shape_param;
            Hardy_param = 0.815*(x_max - x_min) / (double)(m - 1);
            Fasshauer_param = (double)2 / Math.Sqrt(num_of_input_points);

            Interpolate(ref RefTerrain);
        }

        protected override void LoadDimensions(int _Level_Of_Detail, int[] InputSize)
        {
            LOD = _Level_Of_Detail;
            m = (InputSize[0] - 1) * (LOD + 1) + 1;   //rovnake vzorkovanie ako originalne vsupne udaje, pretoze RBF su funkcie a rozdiel od ostatnych implementovanyh ploch
            n = (InputSize[1] - 1) * (LOD + 1) + 1;
            inputSize = new int[2] { InputSize[0],  InputSize[1]};
            num_of_input_points = InputSize[0] * InputSize[1];
        }

        protected override Vector4[,] CreateInterpolationPoints(ref Vector4[,] Vstup)
        {
            SolveSystemToGetWeights(ref Vstup);

            Vector4[,] IP = new Vector4[m, n];

            double dx = (x_max - x_min) / (m - 1);  //krok v x-ovej suradnici
            double dy = (y_max - y_min) / (n - 1);

            double x;
            double y = y_min;
            double z;
            double w = 1;
            for (int j = 0; j < n; j++)
            {
                x = x_min;
                for (int i = 0; i < m; i++)
                {
                   z  = CalculateZ(x, y, Vstup);
                   IP[i, j] = new Vector4((float)x, (float)y, (float)z, (float)w);

                   x += dx;
                }
                y += dy;
            }

            return IP;
        }

        private double CalculateZ(double x, double y, Vector4[,] Vstup)
        {
            double z = 0;
            double dist;

            //nestasne zdlhave riesenie
            //prechadzam pole ako zoznam
            for (int i = 0; i < inputSize[0]; i++)
            {
                for (int j = 0; j < inputSize[1]; j++)
                {
                    dist = EuclidianDist2D(new Vector4((float)x, (float)y, 0, 0), Vstup[i, j]);        

                    z += wieghts[i * inputSize[1]+j] * RBFunction(dist);
                }
            }
            return z;
        }

        private double RBFunction(double distance)
        {
            shape_param = 0.5;
            switch (type_of_basis)
            {
                case (BASIS_FUNCTION.GAUSSIAN):
                    {
                        if (shape_param == 0) return shape_param + 0.000001;
                        return Math.Exp(/*-0.5f* */ -distance * distance * (shape_param* shape_param));
                    }
                case BASIS_FUNCTION.MULTIQUADRATIC:
                    {
                        return Math.Sqrt(1+ distance * distance + shape_param * shape_param);
                    }
                case BASIS_FUNCTION.INVERSE_QUADRATIC:
                    {
                        return 1 / Math.Sqrt(1+distance * distance + shape_param * shape_param);
                    }
                case BASIS_FUNCTION.THIN_PLATE:
                    {
                        if (distance == 0) return 0;
                        return (distance * distance * shape_param * shape_param);
                    }
                default:
                    return 0;
            }
        }

        private double EuclidianDist2D(Vector4 vector1, Vector4 vector2)
        {
            return Math.Sqrt(Math.Pow(vector1.X - vector2.X, 2) + Math.Pow(vector1.Y - vector2.Y, 2));
        }

        private double[,] ComputeRBFunctionValuesForDistances(ref Vector4[,] Vstup, ref double[,] MatrixOfValues)
        {
            //distances = new double[num_of_input_points, num_of_input_points];
            MatrixOfValues = new double[num_of_input_points, num_of_input_points];
            double tempDist = 0, tempRBFVal = 0;

            //TODO prerobit, ak bude cas: najdi najblizsi bod, jeho indexy a okruhom prechadzaj body s najpodobnejsim indexom, ukonci "kruzenie" vtedy, ak prirastok bude mensi ako nejake epsilon

            for (int i = 0; i< inputSize[0]; i++)
            {
                for (int j = 0; j < inputSize[1]; j++)
                {
                    MatrixOfValues[i * inputSize[1] + j, i * inputSize[1] + j] = RBFunction(0);  //diagonala

                    for (int k = i + 1; k < inputSize[0]; k++)
                    {
                        for (int l = j + 1; l < inputSize[1]; l++)
                        {
                            tempDist = EuclidianDist2D(Vstup[i, j], Vstup[k, l]);
                            tempRBFVal = RBFunction(tempDist);
                            MatrixOfValues[i * inputSize[1] + j, k * inputSize[1] + l] = tempRBFVal;
                            MatrixOfValues[k * inputSize[1] + l, i * inputSize[1] + j] = tempRBFVal;
                        }
                    }
                }
            }

            return MatrixOfValues;
        }

        private void SolveSystemToGetWeights(ref Vector4[,] Vstup)
        {
            wieghts = new double[num_of_input_points];
            double[] inOutArray = new double[num_of_input_points];   //=inputsize0*inputsize1
            for (int i = 0; i < inputSize[0]; i++)
            {
                for (int j = 0; j < inputSize[1]; j++)
                {
                    inOutArray[i*inputSize[1]+j] = Vstup[i, j].Z;   //fill z values of point into the array
                }
            }

            //we must solve Aw = z, w=?
            ComputeRBFunctionValuesForDistances(ref Vstup, ref RBFvalues);  //velke PHI
            //inverse matrix
            matinvreport matinvreport = new matinvreport();
            int info = 0;
            rmatrixinverse( ref RBFvalues, num_of_input_points, out info,  out matinvreport);

           // solve A^(-1)*z = w; w =? ZLE SOM POCHOPILA FCIU Z ALGLIB
            rmatrixsolvefast(RBFvalues, num_of_input_points, ref inOutArray, out info);

            for (int i = 0; i < num_of_input_points; i++)
            {
               wieghts[i] = inOutArray[i];   //assign W value (weight) to points from the array
            }
        }
    }
}