using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;   
using System.Windows.Forms.Integration;
using System.IO;


using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace DiplomovaPracaLB
{
    /// <summary>
    /// V Intrepolated Points budu data v pravidelnej mrieyke, zoradene (TODO: pridat triedu pre nepravidelne data)
    /// </summary>

    public abstract class TerrainData
    {
        public Vector4[,] InputDataPoints;      //body na vstupe
        public Vector4[,] InputDataPointsOriginal;      //body s povodnymi vahami
        private Splajn Interpolation;
        public Vector3 posunutie;
        public Matrix3 skalovanie;
        
        protected void Initialize()
        {
            SkopirujUdaje(InputDataPoints, ref InputDataPointsOriginal);
            FindExtremalCoordinates();
        }

        public void ReInterpolate(int new_LOD)
        {
            Interpolation.ChangeLOD(InputDataPoints, new_LOD);
        }

        public void UseKard(float tenstion, int LOD)
        {
            Interpolation = new KardinalnySplajn(InputDataPoints, LOD, tenstion);
        }

        public Vector4[,] GetInterpolationPoints()
        {
            return Interpolation.InterpolationPoints;
        }

        public Vector3[,] GetNormals()
        {
            return Interpolation.Normals;
        }

        public void ResetAllWeights()
        {
            SkopirujUdaje(InputDataPointsOriginal, ref InputDataPoints);    //nacitaju sa povodne
        }

        public void ResetThisWeight(int i, int j)
        {
            InputDataPoints[i,j] = new Vector4(InputDataPointsOriginal[i,j]);
        }

        private void SkopirujUdaje(Vector4[,] Odkial, ref Vector4[,] Kam)
        {
            int a = Odkial.GetLength(0);
            int b = Odkial.GetLength(1);
            Kam = new Vector4[a, b];

            for(int i=0; i<a;i++)
            {
                for (int j = 0;j< b;j++)
                {
                    Kam[i,j] = Odkial[i,j];
                }
            }
        }

        protected void FindExtremalCoordinates()
        {
            float min_z, max_z, min_x, max_x, min_y, max_y;
            float span_x, span_y, span_z, mid_x, mid_y, mid_z;

            min_x = float.MaxValue;
            max_x = float.MinValue;
            min_y = float.MaxValue;
            max_y = float.MinValue;
            min_z = float.MaxValue;
            max_z = float.MinValue;
            for (int j = 0; j < InputDataPoints.GetLength(1); j++)
            {
                for (int i = 0; i < InputDataPoints.GetLength(0); i++)
                {
                    float x = InputDataPoints[i, j].X;
                    float y = InputDataPoints[i, j].Y;
                    float z = InputDataPoints[i, j].Z;

                    if (x < min_x) min_x = x;
                    if (x > max_x) max_x = x;
                    if (y < min_y) min_y = y;
                    if (y > max_y) max_y = y;
                    if (z < min_z) min_z = z;
                    if (z > max_z) max_z = z;
                }
            }
            span_x = max_x - min_x;
            span_y = (max_y - min_y);
            span_z = (max_z - min_z);
            float scale = Math.Max(span_x, Math.Max(span_y, span_z));

            mid_x = (float)(max_x + min_x) / 2;
            mid_y = (float)(max_y + min_y) / 2;
            mid_z = (float)(max_z + min_z) / 2;

            posunutie = new Vector3(-mid_x, -mid_y, -mid_z);
            skalovanie = Matrix3.CreateScale(2 / scale, 2 / scale, 2 / scale);
        }
    }

    public abstract partial class Splajn
    {
        protected int LOD;
        protected int m, n; //pocet vrcholov v zjemnenom vzorkovani      indexy od 0 po m-1
        public Vector4[,] InterpolationPoints;
        public Vector3[,] Normals;              //normalove vektory v lavych dolnych rohov jemneho vzorkovania
        
        protected void LoadInts(Vector4[,] Vstup, int _LOD)
        {
            LOD = _LOD;
            m = (Vstup.GetLength(0) - 2 - 1) * (LOD + 1) + 1; //-2 krajne body z myslienky coonsa odcitam od vstupnych 
            n = (Vstup.GetLength(1) - 2 - 1) * (LOD + 1) + 1;
        }
        
        public void ChangeLOD(Vector4[,] Vstup, int new_LOD)
        {
            LoadInts(Vstup, new_LOD);
            Interpolate(Vstup);
        }

        protected virtual void Interpolate(Vector4[,] Vstup)
        {
            //Kazdy typ splajnu inak
        }

        protected void ComputeNormals(Vector4[,] Vertices)
        {
            Normals = new Vector3[m - 1, n - 1];

            for (int i = 0; i < m - 1; i++)
            {
                for (int j = 0; j < n - 1; j++)
                {
                    Normals[i, j] = ComputeNormalVectorInPoint(Vertices[i, j], Vertices[i, j + 1], Vertices[i + 1, j]);
                }
            }
        }
        
        private Vector3 ComputeNormalVectorInPoint(Vector4 V00, Vector4 V01, Vector4 V10)  //vypocita normalu pre plochu/bod danu vektormi V10-V00 a V01-V00
        {
            //normalove vektory su pocitane pre vsetky stvorceky, kt pocet je v danom smere o 1 menej ako bodov
            Vector3 u = Rational(V10) - Rational(V00);  //musim previes na vahu 1, aby rozdiel bol vektor
            Vector3 v = Rational(V01) - Rational(V00);
            Vector3 c = Vector3.Cross(u, v);
            //niekedy su vrcholy usporiadane zostupne a niekedy opacne v datasete, ale chcem aby normaly smerovali "hore. nemam taky pripad datasetu, ze by tam boli previsy
            //test, ci zvieraju ostry uhol, tj ked skal.sucin je >0 (ked =0, tak je jedno)
            if (Vector3.Dot(c, Vector3.UnitZ) < 0)
            {
                c = Vector3.Cross(v, u);
            }
            c.Normalize();
            return c;
        }

        public Vector3 Rational(Vector4 vertex)
        {
            //z homogennych do afinnych suradnic
            if (vertex[3] == 0)
            {
                MessageBox.Show("pozor, bod ma nulovu vahu");
                return Vector3.Zero;
            }
            if (vertex[3] == 1)
            {
                return new Vector3(vertex);     //len za zabudne w-suradnica
            }

            return new Vector3(vertex[0] / vertex[3], vertex[1] / vertex[3], vertex[2] / vertex[3]);
        }
    }

    public class KardinalnySplajn : Splajn
    {
        float tension;

        public KardinalnySplajn(Vector4[,] Vstup, int _LOD, float _tension)
        {
            LoadInts(Vstup, _LOD);
            
            tension = _tension;

            Interpolate(Vstup);
        }


        protected override void Interpolate(Vector4[,] Vstup)
        {
            InterpolationPoints = CreateInterpolationPoints(Vstup);
            ComputeNormals(InterpolationPoints);
        }

        private Vector4[,] CreateInterpolationPoints(Vector4[,] Vstup)
        {
            Vector4[,] IP = new Vector4[m, n];
            float s = tension;
            Matrix4 LocalControlPoints;

                        //predvypocitam hodnoty hermitovych bazickych funkcii pre rovnomerne rozmiestnene body na krivke. budem ich pouzivat pri kazdej zaplatke
            Vector4[] Hu = new Vector4[LOD];    //v riadkoch su ulozene vycislene hodnoty 4 zmiesavacich fcii , v kazdom riadku je ina hodnota parametra, v kazdom stlpci rovnaka zmiesavacia fcia
            Matrix4 H = new Matrix4(0, 1, 0, 0, -s, 0, s, 0, 2 * s, s - 3, 3 - 2 * s, -s, -s, 2 - s, s - 2, s);
            for (int i = 1; i <= LOD; i++)    //0 a lOD+1 su okrajove, kt. pozname
            {
                float t = i / (float)(LOD + 1);   //parameter s je z <0,1>
                Vector4 u = new Vector4(1, t, t * t, t * t * t);
                Hu[i - 1] = MyMultiply(u, H);
            }

            IP[0, 0] = Vstup[1, 1]; //prvy bod interpolacie
            for (int j = 1; j < Vstup.GetLength(1) - 2; j++)
            {
                for (int i = 1; i < Vstup.GetLength(0) - 2; i++)
                {
                    int a = (i - 1) * (LOD + 1); //index laveho dolneho rohu v IntrepolacncyhBodoch
                    int b = (j - 1) * (LOD + 1);
                    //teraz vytvorím mini záplatu

                    //Okrajové body
                    Vector4 C00 = Vstup[i, j];
                    Vector4 C01 = Vstup[i, j + 1];
                    Vector4 C10 = Vstup[i + 1, j];
                    Vector4 C11 = Vstup[i + 1, j + 1];

                    //zapamatam si ich do vysledku

                    IP[a + LOD + 1, b] = C10;
                    IP[a, b + LOD + 1] = C01;
                    IP[a + LOD + 1, b + LOD + 1] = C11; //TODO toto je neefektivne, opakovane sa zapisuje rovnaka hodnota



                    //hodnoty Hermitovych krivkovych splajnov na okrajoch zaplatky, vypocitane v yjemnenych bodoch
                    for (int k = 0; k < LOD; k++)
                    {
                        //uplne prve krajne segmenty c0 a d0
                        if (j == 1)
                        {
                            LocalControlPoints = new Matrix4(Vstup[i - 1, j], Vstup[i, j], Vstup[i + 1, j], Vstup[i + 2, j]);
                            IP[a + k + 1, 0] = MyMultiply(Hu[k], LocalControlPoints);
                        }

                        if (i == 1)
                        {
                            LocalControlPoints = new Matrix4(Vstup[i, j - 1], Vstup[i, j], Vstup[i, j + 1], Vstup[i, j + 2]);
                            IP[0, b + k + 1] = MyMultiply(Hu[k], LocalControlPoints);
                        }

                        //vypocet segmentov c1,d1 (v dalsom kroku to budu c0, d0)
                        LocalControlPoints = new Matrix4(Vstup[i - 1, j + 1], Vstup[i, j + 1], Vstup[i + 1, j + 1], Vstup[i + 2, j + 1]);
                        IP[a + 1 + k, b + LOD + 1] = MyMultiply(Hu[k], LocalControlPoints);

                        LocalControlPoints = new Matrix4(Vstup[i + 1, j - 1], Vstup[i + 1, j], Vstup[i + 1, j + 1], Vstup[i + 1, j + 2]);
                        IP[a + LOD + 1, b + 1 + k] = MyMultiply(Hu[k], LocalControlPoints);
                    }


                    //Coonsova zaplata C0napojenie
                    // = zaplata s okraojvymi krivkami c0 c1 d0, d1 (oproti sebe rovnake picmenka)

                    //na Coonsovu zaplatu potrebujeme 3 podzaplaty:
                    //priamkova zaplata IZAP - interpoluje krivky C
                    //priamkova zaplata JZAP - interpoluje krivky D
                    //korekcna zaplata, ktora bilinearne interpoluje rohy zaplaty

                    for (int l = 1; l <= LOD; l++)
                    {
                        float q = l / (float)(LOD + 1);

                        for (int k = 1; k <= LOD; k++)
                        {
                            float p = k / (float)(LOD + 1);

                            Vector4 LinInterpJZap = (1 - p) * IP[a, b + l] + p * IP[a + LOD + 1, b + l];
                            Vector4 LinInterpIZap = (1 - q) * IP[a + k, b] + q * IP[a + k, b + LOD + 1];

                            Vector4 KorekcnaZap = (1 - p) * (1 - q) * C00 + p * (1 - q) * C10 + (1 - p) * q * C01 + p * q * C11;

                            IP[a + k, b + l] = LinInterpIZap + LinInterpJZap - KorekcnaZap;
                            //Console.WriteLine(IP[a+k, b+l]);
                        }
                    }
                }
            }

            return IP;
        }
    }
}
