using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private float x_min, x_max, y_min, y_max;   //toto tu nebude musiet byt, ak urobis splajny cez enumerat
        private int num_of_input_points;

        public float shape_param;
        
        private BASIS_FUNCTION typ_of_basis;
        
        private float[,] distances; // vzdialenosti pre kazde 2 body budu rovnake pre rovnaky vstup - zavisi od vstupu
        private float[,] RBFvalues; //hodnoty vzialenodstnej funkcie pre kazde 2 body - zavisi od vstupu a voľby bázy
      

        public SplajnRadialBasis(Vector4[,] Vstup, int _LOD, BASIS_FUNCTION typ, float input_shape_param, float xmin, float xmax, float ymin, float ymax)
        {
            
            LoadDimensions(_LOD, Vstup);

            x_min = xmin;
            x_max = xmax;
            y_min = ymin;
            y_max = ymax;

            typ_of_basis = typ;
            shape_param = input_shape_param;
            
            //ComputeDistancesForEachTwoInputPoints(Vstup); 

            Interpolate(Vstup);

        }

        protected override void LoadDimensions(int _Level_Of_Detail, Vector4[,] Vstup)
        {
            LOD = _Level_Of_Detail;
            m = (M - 1) * (LOD + 1) + 1;   //rovnake vzorkovanie ako originalne vsupne udaje, pretoze RBF su funkcie a rozdiel od ostatnych implementovanyh ploch
            n = (N - 1) * (LOD + 1) + 1;
            num_of_input_points = m * n;
        }

        protected override Vector4[,] CreateInterpolationPoints(Vector4[,] Vstup)
        {
            //ComputeRBFunctionValuesForDistances(Vstup);  //velke PHI

            Vector4[,] IP = new Vector4[m, n];

            float x, y, z, w;
            float delta_x = (x_max - x_min) / (m - 1);  //krok v x-ovej suradnici
            float delta_y = (y_max - y_min) / (n - 1);


            for (int j = 0; j < n; j++)
            {
                y = y_min + j * delta_y;
                for (int i = 0; i < m; i++)
                {
                    w = 1;
                    x = x_min + i * delta_x;
                    z = CalculateZ(x, y, Vstup);

                    IP[i, j] = new Vector4(x, y, z, w);
                }
            }

            return IP;
        }

        private float CalculateZ(float x, float y, Vector4[,] Vstup)
        {

            float z = 0;
            float dist;

            //nestasne zdlhave riesenie
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    dist = EuclidianDist2D(new Vector4(x, y, 0, 0), Vstup[i, j]);        //prechadzam pole ako zoznam

                    z += Vstup[i, j].W * RBFunction(dist);
                }
            }


            return z;
        }

        private float RBFunction(float distance)
        {
            switch (typ_of_basis)
            {
                case (BASIS_FUNCTION.GAUSSIAN):
                    {
                        return (float)Math.Exp(-0.5f * Math.Pow(distance, 2) / Math.Pow(shape_param, 2));
                    }
                case BASIS_FUNCTION.MULTIQUADRATIC:
                    {
                        return (float)Math.Sqrt(Math.Pow(distance, 2) + Math.Pow(shape_param, 2));
                    }
                case BASIS_FUNCTION.INVERSE_QUADRATIC:
                    {
                        return 1/(float)Math.Sqrt(Math.Pow(distance, 2) + Math.Pow(shape_param, 2));
                    }
                    case BASIS_FUNCTION.THIN_PLATE:
                    {
                        return (float)(Math.Pow(distance, 2)*Math.Log(distance));
                    }
                default:
                    return 0;
            }
        }

        private float EuclidianDist2D(Vector4 vector1, Vector4 vector2)
        {
            return (float)Math.Sqrt(Math.Pow(vector1.X - vector2.X, 2) + Math.Pow(vector1.Y - vector2.Y, 2));
        }
    }
}
