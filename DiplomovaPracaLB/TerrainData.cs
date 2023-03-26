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
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.IO;
using System.Drawing;

using msg = System.Windows;   //vytvorím alias pre namespace, lebo 2 rozne namespacy maju clasu s rovnakym nazvom

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Dynamic;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Runtime.InteropServices.WindowsRuntime;

namespace DiplomovaPracaLB
{
    /// <summary>
    /// V Intrepolated Points budu data v pravidelnej mrieyke, zoradene (TODO: pridat triedu pre nepravidelne data)
    /// </summary>

    public enum typInterpolacie
    {
        NEINTERPOLUJ,
        CATMULLROM,
        TypA,
        TypB
    };

    public class TerrainData
    {
        public Vector3[,] InputDataPoints;      //body na vstupe
        public Vector3[,] InterpolationPoints;  //body vyslednej plochy = jemne vzorkovanie
        public Vector3[,] Normals;              //normalove vektory v lavych dolnych rohov jemneho vzorkovania
        protected int m, n; //pocet vrcholov v zjemnenom vzorkovani      indexy od 0 po m-1

        public Vector3 posunutie;
        public Matrix3 skalovanie;
        private typInterpolacie selectedInterpolationType;

        protected void Inicialize(typInterpolacie _typInterpolacie, int LOD) //1. inicialzuje sa materska klasa, 2. inicializuje sa dcerska klasa, 3. do inicializacie dcery vlozim tuto funkciu - su v nej veci an vykonanie na konci kazdeh Terrain Data
        {
            //InputDataPoints su uz nacitane z dcerskej triedy

            //data su od -1 po m+1, lebo krajne body
            //pozerám sa na vnútorné susedné štvorice vstupného datasetu
            m = (InputDataPoints.GetLength(0) - 2 - 1) * (LOD + 1) + 1; //-2 krajne body z myslienky coonsa odcitam od vstupnych 
            n = (InputDataPoints.GetLength(1) - 2 - 1) * (LOD + 1) + 1;

            FindExtremalCoordinates();

            selectedInterpolationType = _typInterpolacie;
            InterpolationPoints = Interpoluj(_typInterpolacie, InputDataPoints, LOD);
            Normals = ComputeNormals(InterpolationPoints);
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

        public void ReInterpolate(int new_LOD)
        {
            m = (InputDataPoints.GetLength(0) - 2 - 1) * (new_LOD + 1) + 1; //-2 krajne body z myslienky coonsa odcitam od vstupnych 
            n = (InputDataPoints.GetLength(1) - 2 - 1) * (new_LOD + 1) + 1;
            InterpolationPoints = Interpoluj(selectedInterpolationType, InputDataPoints, new_LOD);
            Normals = ComputeNormals(InterpolationPoints);
        }

        private Vector3[,] Interpoluj(typInterpolacie _typ, Vector3[,] Vstup, int LOD)
        {
            Vector3[,] IP = new Vector3[m, n];

            if (_typ == typInterpolacie.CATMULLROM)   //catmull+ bilinearny Coons pre kazdy stvorcek
            {
                //TODO hermitov splajn sa da spravit pyramidovo, teda rychlejsie a lepsie

                //paramter pnutia
                float s = 0.5f; //Kardinalny splajn pre hodnotu 1/2 je Catmull-Rom

                //predvypocitam hodnoty hermitovych bazickych funkcii pre rovnomerne rozmiestnene body na krivke. budem ich pouzivat pri kazdej zaplatke
                float[,] H = new float[LOD, 4];
                for (int i = 1; i <= LOD; i++)
                {
                    float t = i / (float)(LOD + 1);   //parameter z <0,1>
                    H[i - 1, 0] = -s * t + 2 * s * t * t - s * t * t * t;
                    H[i - 1, 1] = 1 + (s - 3) * t * t + (2 - s) * t * t * t;
                    H[i - 1, 2] = s * t + (3 - 2 * s) * t * t + (s - 2) * t * t * t;
                    H[i - 1, 3] = -s * t * t + s * t * t * t;
                }
                
                float x, y, z;

                IP[0, 0] = Vstup[1, 1]; //prvy bod interpolacie
                for (int j = 1; j < Vstup.GetLength(1) - 2; j++)
                {
                    for (int i = 1; i < Vstup.GetLength(0) - 2; i++)
                    {
                        int a = (i - 1) * (LOD + 1); //index laveho dolneho rohu v IntrepolacncyhBodoch
                        int b = (j - 1) * (LOD + 1);
                        //teraz vytvorím mini záplatu

                        //Okrajové body
                        Vector3 C00 = Vstup[i, j];
                        Vector3 C01 = Vstup[i, j + 1];
                        Vector3 C10 = Vstup[i + 1, j];
                        Vector3 C11 = Vstup[i + 1, j + 1];

                        //zapamatam si ich do vysledku

                        IP[a + LOD + 1, b] = C10;
                        IP[a, b + LOD + 1] = C01;
                        IP[a + LOD + 1, b + LOD + 1] = C11; //TODO toto je neefektivne, opakovane sa zapisuje rovnaka hodnota


                        //hodnoty Hermitovych splajnov na okrajoch zaplatky, vypocitane v yjemnenych bodoch
                        for (int k = 0; k < LOD; k++)
                        {
                            //uplne prve krajne segmenty c0 a d0
                            if (j == 1)
                            {
                                x = H[k, 0] * Vstup[i - 1, j].X + H[k, 1] * Vstup[i, j].X + H[k, 2] * Vstup[i + 1, j].X + H[k, 3] * Vstup[i + 2, j].X;
                                y = H[k, 0] * Vstup[i - 1, j].Y + H[k, 1] * Vstup[i, j].Y + H[k, 2] * Vstup[i + 1, j].Y + H[k, 3] * Vstup[i + 2, j].Y;
                                z = H[k, 0] * Vstup[i - 1, j].Z + H[k, 1] * Vstup[i, j].Z + H[k, 2] * Vstup[i + 1, j].Z + H[k, 3] * Vstup[i + 2, j].Z;

                                IP[a + k + 1, 0] = new Vector3(x, y, z);
                            }

                            if (i == 1)
                            {
                                x = H[k, 0] * Vstup[i, j - 1].X + H[k, 1] * Vstup[i, j].X + H[k, 2] * Vstup[i, j + 1].X + H[k, 3] * Vstup[i, j + 2].X;
                                y = H[k, 0] * Vstup[i, j - 1].Y + H[k, 1] * Vstup[i, j].Y + H[k, 2] * Vstup[i, j + 1].Y + H[k, 3] * Vstup[i, j + 2].Y;
                                z = H[k, 0] * Vstup[i, j - 1].Z + H[k, 1] * Vstup[i, j].Z + H[k, 2] * Vstup[i, j + 1].Z + H[k, 3] * Vstup[i, j + 2].Z;

                                IP[0, b + k + 1] = new Vector3(x, y, z);
                            }

                            //vypocet segmentov c1,d1 (v dalsom kroku to budu c0, d0)
                            j++;
                            x = H[k, 0] * Vstup[i - 1, j].X + H[k, 1] * Vstup[i, j].X + H[k, 2] * Vstup[i + 1, j].X + H[k, 3] * Vstup[i + 2, j].X;
                            y = H[k, 0] * Vstup[i - 1, j].Y + H[k, 1] * Vstup[i, j].Y + H[k, 2] * Vstup[i + 1, j].Y + H[k, 3] * Vstup[i + 2, j].Y;
                            z = H[k, 0] * Vstup[i - 1, j].Z + H[k, 1] * Vstup[i, j].Z + H[k, 2] * Vstup[i + 1, j].Z + H[k, 3] * Vstup[i + 2, j].Z;
                            j--;
                            IP[a + 1 + k, b + LOD + 1] = new Vector3(x, y, z);

                            i++;
                            x = H[k, 0] * Vstup[i, j - 1].X + H[k, 1] * Vstup[i, j].X + H[k, 2] * Vstup[i, j + 1].X + H[k, 3] * Vstup[i, j + 2].X;
                            y = H[k, 0] * Vstup[i, j - 1].Y + H[k, 1] * Vstup[i, j].Y + H[k, 2] * Vstup[i, j + 1].Y + H[k, 3] * Vstup[i, j + 2].Y;
                            z = H[k, 0] * Vstup[i, j - 1].Z + H[k, 1] * Vstup[i, j].Z + H[k, 2] * Vstup[i, j + 1].Z + H[k, 3] * Vstup[i, j + 2].Z;
                            i--;
                            IP[a + LOD + 1, b + 1 + k] = new Vector3(x, y, z);
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

                                Vector3 LinInterpJZap = (1 - p) * IP[a, b + l] + p * IP[a + LOD + 1, b + l];
                                Vector3 LinInterpIZap = (1 - q) * IP[a + k, b] + q * IP[a + k, b + LOD + 1];

                                Vector3 KorekcnaZap = (1 - p) * (1 - q) * C00 + p * (1 - q) * C10 + (1 - p) * q * C01 + p * q * C11;

                                IP[a + k, b + l] = LinInterpIZap + LinInterpJZap - KorekcnaZap;
                                //Console.WriteLine(IP[a+k, b+l]);
                            }
                        }
                    }
                }

                return IP;
            }

            //if (_typ == typInterpolacie.NEINTERPOLUJ) return Vstup;
            return Vstup;
        }

        private Vector3[,] ComputeNormals(Vector3[,] Vertices)
        {

            Vector3[,] Norm = new Vector3[m - 1, n - 1];

            for (int i = 0; i < m - 1; i++)
            {
                for (int j = 0; j < n - 1; j++)
                {
                    Norm[i, j] = ComputeNormalVectorInPoint(Vertices[i, j], Vertices[i, j + 1], Vertices[i + 1, j]);
                    Norm[i, j].Normalize();
                }
            }

            return Norm;
        }

        private Vector3 ComputeNormalVectorInPoint(Vector3 V00, Vector3 V01, Vector3 V10)  //vypocita normalu pre plochu/bod danu vektormi V10-V00 a V01-V00
        {
            //normalove vektory su pocitane pre vsetky stvorceky, kt pocet je v danom smere o 1 menej ako bodov
            Vector3 u = V10 - V00;
            Vector3 v = V01 - V00;
            Vector3 c = Vector3.Cross(u, v);
            c.Normalize();
            return c;
        }
    }
}
