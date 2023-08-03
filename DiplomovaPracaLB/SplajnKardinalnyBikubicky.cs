using OpenTK;
using System.Runtime.CompilerServices;

namespace DiplomovaPracaLB
{
    public class SplajnKardinalnyBikubicky : Splajn
    {
        float tension;

        public SplajnKardinalnyBikubicky(Vector4[,] Vstup, int _LOD, float _tension)
        {
            tension = (1 - _tension) / 2;   //prirodzenejsi priebeh parametra z knihy D. Salomona
            LoadDimensions(_LOD, Vstup);
            Interpolate(Vstup);
        }

        protected override void LoadDimensions(int _Level_Of_Detail, Vector4[,] Vstup)
        {
            LOD = _Level_Of_Detail;
            m = (M - 4 - 1) * (LOD + 1) + 1; //-4 krajne body z myslienky Anidiho twistov Coonsa odcitam od vstupnych 
            n = (N - 4 - 1) * (LOD + 1) + 1;
        }

        protected override Vector4[,] CreateInterpolationPoints(Vector4[,] Vstup)
        {
            Vector4[,] IP = new Vector4[m, n];

            //vzorkovanie  - Hermitove funkcie budu mat rovnake hodnoty na kazdej patch
            Matrix4 H = new Matrix4(1, 0, 0, 0, 0, 0, 1, 0, -3, 3, -2, -1, 2, -2, 1, 1);    //koeficienty zmiesavacich fcii
            Vector4[] Ht = new Vector4[LOD + 2];    //po riadkoch vycislene zmiesavacie fcie v hodnotach intervalu [0, 1] = [0, 1, 2, ..., LOD+1]/(LOD+1)
            for (int i = 0; i <= LOD + 1; i++)    //0 a lOD+1 su okraje
            {
                float t = i / (float)(LOD + 1);   //parameter s je z <0,1>
                Vector4 valuesOfParam = new Vector4(1, t, t * t, t * t * t);
                Ht[i] = MyMultiply(valuesOfParam, H);
            }

            float x, y, z, w;

            for (int j = 2; j < N - 3; j++)
            {
                for (int i = 2; i < M - 3; i++)
                {
                    int a = (i - 2) * (LOD + 1); //index laveho dolneho rohu v IntrepolacncyhBodoch
                    int b = (j - 2) * (LOD + 1);

                    Matrix4[] Matica_P_Udajov = BuildBicubicCoonsMatrix(Vstup, i, j);

                    //hodnoty Hermitovych krivkovych splajnov na okrajoch zaplatky, vypocitane v yjemnenych bodoch
                    for (int k = 0; k <= LOD + 1; k++)
                    {
                        for (int l = 0; l <= LOD + 1; l++)   //skarede - uz 4. vnoreny cyklus
                        {
                            w = Vector4.Dot(MyMultiply(Ht[k], Matica_P_Udajov[3]), Ht[l]);
                            x = Vector4.Dot(MyMultiply(Ht[k], Matica_P_Udajov[0]), Ht[l]);
                            y = Vector4.Dot(MyMultiply(Ht[k], Matica_P_Udajov[1]), Ht[l]);
                            z = Vector4.Dot(MyMultiply(Ht[k], Matica_P_Udajov[2]), Ht[l]);

                            IP[a + k, b + l] = new Vector4(x, y, z, w);
                        }
                    }
                }
            }

            return IP;
        }

        private Matrix4[] BuildBicubicCoonsMatrix(Vector4[,] Vstup, int i, int j)     //po suradniciach x,y,z, w
        {
            Matrix4[] P = new Matrix4[4];

            Vector4 P00 = Vstup[i, j];
            Vector4 P01 = Vstup[i, j + 1];
            Vector4 P10 = Vstup[i + 1, j];
            Vector4 P11 = Vstup[i + 1, j + 1];
            Vector4 Pu00 = TangentVectorU(Vstup, i, j);
            Vector4 Pu01 = TangentVectorU(Vstup, i, j + 1);
            Vector4 Pu10 = TangentVectorU(Vstup, i + 1, j);
            Vector4 Pu11 = TangentVectorU(Vstup, i + 1, j + 1);
            Vector4 Pv00 = TangentVectorV(Vstup, i, j);
            Vector4 Pv01 = TangentVectorV(Vstup, i, j + 1);
            Vector4 Pv10 = TangentVectorV(Vstup, i + 1, j);
            Vector4 Pv11 = TangentVectorV(Vstup, i + 1, j + 1);

            Vector4 Puv00 = AdiniTwist(Vstup, i, j);
            Vector4 Puv01 = AdiniTwist(Vstup, i, j + 1);
            Vector4 Puv10 = AdiniTwist(Vstup, i + 1, j);
            Vector4 Puv11 = AdiniTwist(Vstup, i + 1, j + 1);
            /*
             * Nulove twisty
            Vector4 Puv00 = new Vector4(Vector3.Zero,1.0f);
            Vector4 Puv01 = new Vector4(Vector3.Zero, 1.0f);
            Vector4 Puv10 = new Vector4(Vector3.Zero, 1.0f);
            Vector4 Puv11 = new Vector4(Vector3.Zero, 1.0f);
            */
            P[0] = new Matrix4(P00.X, P01.X, Pv00.X, Pv01.X, P10.X, P11.X, Pv10.X, Pv11.X, Pu00.X, Pu01.X, Puv00.X, Puv01.X, Pu10.X, Pu11.X, Puv10.X, Puv11.X);
            P[1] = new Matrix4(P00.Y, P01.Y, Pv00.Y, Pv01.Y, P10.Y, P11.Y, Pv10.Y, Pv11.Y, Pu00.Y, Pu01.Y, Puv00.Y, Puv01.Y, Pu10.Y, Pu11.Y, Puv10.Y, Puv11.Y);
            P[2] = new Matrix4(P00.Z, P01.Z, Pv00.Z, Pv01.Z, P10.Z, P11.Z, Pv10.Z, Pv11.Z, Pu00.Z, Pu01.Z, Puv00.Z, Puv01.Z, Pu10.Z, Pu11.Z, Puv10.Z, Puv11.Z);
            P[3] = new Matrix4(P00.W, P01.W, Pv00.W, Pv01.W, P10.W, P11.W, Pv10.W, Pv11.W, Pu00.W, Pu01.W, Puv00.W, Puv01.W, Pu10.W, Pu11.W, Puv10.W, Puv11.W);

            return P;
        }

        private Vector4 AdiniTwist(Vector4[,] Vstup, int i, int j)
        {
            //float delta = 2 = 1+1 = dlzka 2 intervalov
            Vector4 E = -TangentVectorV(Vstup, i, j - 1) + TangentVectorV(Vstup, i, j + 1); // /2
            Vector4 F = -TangentVectorU(Vstup, i - 1, j) + TangentVectorU(Vstup, i + 1, j); // /2
            Vector4 G = -Vstup[i - 1, j - 1] - Vstup[i + 1, j + 1] + Vstup[i - 1, j + 1] + Vstup[i + 1, j - 1]; // /(2*2)

            return (E + F) / 2 + G / 4;
        }

        //pri výpočte vektorov preba prejsť do nehomogénnych súradníc, aby váha vektora bola = 0!
        private Vector4 TangentVectorU(Vector4[,] Vstup, int i, int j)
        {
            //vektor nech je vektor, tak nech je vaha nula
            return tension * new Vector4(Rational(Vstup[i + 1, j]) - Rational(Vstup[i - 1, j]), 0.0f);
        }

        private Vector4 TangentVectorV(Vector4[,] Vstup, int i, int j)
        {
            return tension * new Vector4(Rational(Vstup[i, j + 1]) - Rational(Vstup[i, j - 1]), 0.0f);
        }
    }
}
