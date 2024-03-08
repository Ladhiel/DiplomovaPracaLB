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
        protected Vector4[,] DataPointsAll;    //cely dataset bodov
        private Vector4[,] DataPointsSample;  //body na vstupe - z ODP vybraty kadzny k-ty podla dentsity, s povodnymi vahami
        public Vector4[,] WeightedDataPointsSample;          //body na vstupe - z ODP vybraty kadzny k-ty podla dentsity

        //private Splajn Interpolation;
        public Vector3 posunutie;
        public Matrix3 skalovanie;

        private int density = 10;  //hustota podmnoziny datasetu    
        private int[] border = new int[2];   //hranicne indexy pre porovnavaciu mriezku

        float min_z, max_z, min_x, max_x, min_y, max_y;

        protected void Initialize()
        {
            //OriginalData su uz nacitane
            WeightedDataPointsSample = SelectSampleFromOrigData();
            ZalohujSample();
            FindExtremalCoordinates();
        }

        private Vector4[,] SelectSampleFromOrigData()
        {
            //zapamatam ohranicenie mriezky, z kt. vyberam sample
            int a = (DataPointsAll.GetLength(0) - 1) / density; //vyuzivam celociselne delenie
            int b = (DataPointsAll.GetLength(1) - 1) / density;

            border[0] = density * a;
            border[1] = density * b;

            Vector4[,] IDP = new Vector4[a + 1, b + 1];

            for (int i = 0; i * density < DataPointsAll.GetLength(0); i++)
            {
                for (int j = 0; j * density < DataPointsAll.GetLength(1); j++)
                {
                    IDP[i, j] = DataPointsAll[i * density, j * density];
                }
            }

            return IDP;
        }

       public void SetWeight(int active_m_index, int active_n_index, float new_weight)
        {
            WeightedDataPointsSample[active_m_index, active_n_index].X = new_weight * DataPointsSample[active_m_index, active_n_index].X;
            WeightedDataPointsSample[active_m_index, active_n_index].Y = new_weight * DataPointsSample[active_m_index, active_n_index].Y;
            WeightedDataPointsSample[active_m_index, active_n_index].Z = new_weight * DataPointsSample[active_m_index, active_n_index].Z;
            WeightedDataPointsSample[active_m_index, active_n_index].W = new_weight;
        }

       public void ResetWeight(int i, int j)
        {
            WeightedDataPointsSample[i, j] = new Vector4(DataPointsSample[i, j]);
        }
        private void ZalohujSample()
        {
            SkopirujUdaje(ref WeightedDataPointsSample, ref DataPointsSample);
        }
        public void ObnovSample()
        {
            SkopirujUdaje(ref DataPointsSample, ref WeightedDataPointsSample);    //nacitaju sa povodne
        }

        private void SkopirujUdaje(ref Vector4[,] Odkial, ref Vector4[,] Kam)
        {
            int a = Odkial.GetLength(0);
            int b = Odkial.GetLength(1);
            Kam = new Vector4[a, b];

            for (int i = 0; i < a; i++)
            {
                for (int j = 0; j < b; j++)
                {
                    Kam[i, j] = new Vector4(Odkial[i, j]);
                }
            }
        }
        protected void FindExtremalCoordinates()
        {
            
            float span_x, span_y, span_z, mid_x, mid_y, mid_z;

            min_x = float.MaxValue;
            max_x = float.MinValue;
            min_y = float.MaxValue;
            max_y = float.MinValue;
            min_z = float.MaxValue;
            max_z = float.MinValue;
            for (int j = 0; j < WeightedDataPointsSample.GetLength(1); j++)
            {
                for (int i = 0; i < WeightedDataPointsSample.GetLength(0); i++)
                {
                    float x = WeightedDataPointsSample[i, j].X;
                    float y = WeightedDataPointsSample[i, j].Y;
                    float z = WeightedDataPointsSample[i, j].Z;

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

        private void FindClosestSquare(float x, float y, ref int out_i, ref int out_j)
        {
            //vychadzam z toho, ze vstupne udaje su v pravidelnej mriezke

            float span_x = Math.Abs(max_x - min_x);
            float dielik_x = span_x / DataPointsAll.GetLength(0);
            float temp_x = Math.Abs(x - min_x);
            out_i = (int)Math.Floor(temp_x / dielik_x);


            float span_y = Math.Abs(max_y - min_y);
            float dielik_y = span_y / DataPointsAll.GetLength(1);
            float temp_y = Math.Abs(y - min_y);
            out_j = (int)Math.Floor(temp_y / dielik_y);
        }

        private void FindClosestSquarever2(float x, float y, ref int out_i, ref int out_j)
        {
            //vychadzam z toho, ze vstupne udaje su v pravidelnej mriezke
            
            float span_x = Math.Abs(max_x - min_x);
            float dielik_x = span_x / DataPointsAll.GetLength(0);
            float temp_x = Math.Abs(x - min_x);
            out_i = (int)Math.Floor(temp_x / dielik_x);


            float span_y = Math.Abs(max_y - min_y);
            float dielik_y = span_y / DataPointsAll.GetLength(1);
            float temp_y = Math.Abs(y - min_y);
            out_j = (int)Math.Floor(temp_y / dielik_y);
        }

        public float GetApproximateZver2(float x, float y)
        {
            //presnejsie, ale neriesi problem

            float approx_z = 0.0f;

            int i = 0, j = 0;
            float dielik_x = (max_x - min_x) / DataPointsAll.GetLength(0);
            float dielik_y = (max_y - min_y) / DataPointsAll.GetLength(1);
            FindClosestSquarever2(x, y, ref i, ref j);

            //TODO toto sa mozno hodi aj do textu:
            //mame stvorcek [i,j]. pripominame, ze je to "stvorcek", jebo je neplanarny!
            //skusila som robit vyhodnotenie s vyuzitim iba dotykovej roviny 2 hran, ale to vyustovalo do toho, ze vo interpolovanyh bodoch (naprotivny roh) nebola hodnota rozdielu splajnu a datasetu nulova, tak ako by mala byt.
            //Preto rozdelim neplanarny stvorek na 4 planarne trouholnicky, ktore lepsie aproximuju tvar porovnavaneh datasetu:
            //storvcek [i,j] rozdelim na 4 trojuholniky, ktorych zakladnami su hrany stvoreka a 3. vrhol maju v strede stvoreka.
            //stylom diamant-stvorec stredny bod:

            Vector3 MidP = new Vector3((DataPointsAll[i, j] + DataPointsAll[i + 1, j] + DataPointsAll[i + 1, j + 1] + DataPointsAll[i, j + 1]) / 4);

            //test: ktory z trojuholnikov ma bod s tymito (x,y)?
            /*
            y
            ^
            |
            |
        [i,j+1]---------------[i+1,j+1]
            | \2.   D     / |
            |   \       /   |
            |     \   /     |
            |  A    M     C |
            |     /   \     |
            | 1./       \   |
            | /     B     \ |
        [i,j] ---------------[i+1,j]--->x
                */

            //testujem bod polohu (x,y) voci diagonalam
            //pre x najdeme body na diagonalach (x, y1) (x, y2)
            //usetrim testovanie ci je bod vonku, lebo odpoveda na tu otazku poznam (nie).

            //predpokladam ze body datasetu su zoradene vzostupne od najmensej po najvyssiu suradnicu v oboh dimenziach
            double lokal_x = DataPointsAll[i, j].X - x;
            double lokal_y = DataPointsAll[i, j].Y - y;
            double y1 = dielik_y * lokal_x / dielik_x;  //na diagonale spajajucej [i,j] a [i+1, j+1]
            double y2 = dielik_y - y1;                  //na diagonale spajajucej [i+1, j] a [i, j+1] 

            Vector3 u, v;
            if (y < y1)   //je blizsie ku [i+1,j] nez ku {i,j+1}?
            {
                //B alebo C
                u = new Vector3(DataPointsAll[i, j + 1]) - MidP;
            }
            else
            {
                //A alebo D
                u = new Vector3(DataPointsAll[i + 1, j]) - MidP;
            }

            if (y < y2) //je blizsie ku [i,j] nez ku [i+1, j+1]?
            {
                v = new Vector3(DataPointsAll[i, j]) - MidP;
            }
            else
            {
                v = new Vector3(DataPointsAll[i + 1, j + 1]) - MidP;
            }

            //prienik roviny a priamky
            //rovina je dana bodom P a vektormi u,v - resp P a normalou n 
            //tiez nemusim testova ohranicenia trojuholnika, lebo cez FindClosestSquare som vybrala jediny objekt, ktory ma bod so suradnicami (x,y, nieo)
            //0=Dot(n, X-P) = n1*(x-p1)+n2*(y-p2)+n3*(z-p3) <-aby X bolo z roviny stvorceka
            //-(z-p3)=(n1*(x-p1)+n2*(y-p2))/n3 

            Vector3 n = Vector3.Cross(u, v);    //nezalezi na orientacii trojuholnika, kedze testujem len ci lezi na rovine
            approx_z = -(n.X * (x - MidP.X) + n.X * (y - MidP.Y)) / n.Z + MidP.Z;

            return approx_z;
        }

        public float GetApproximateZ(float x, float y)
        {
            float approx_z = 0.0f;
            int i = 0, j = 0;
            FindClosestSquare(x, y, ref i, ref j);

            Vector3 P = new Vector3(DataPointsAll[i,j]);
            Vector3 u = new Vector3(DataPointsAll[i, j] - DataPointsAll[i + 1, j]);
            Vector3 v = new Vector3(DataPointsAll[i, j] - DataPointsAll[i, j + 1]);
            Vector3 n = Vector3.Cross(u, v);    //nezalezi na orientacii trojuholnika, kedze testujem len ci lezi na rovine
            
            approx_z = -(n.X * (x - P.X) + n.X * (y - P.Y)) / n.Z + P.Z;
          
            return approx_z;
        }

        //-----Getters------------------------------------------------------------------

        public float GetRealZ(int i, int j)
        {
            return DataPointsAll[i, j].Z;
        }

        public void SetDensity(int new_density)
        {
            if(0<=new_density && new_density <=20);
            {
                density = new_density;
            }
        }

        public int[] GetSampleSize()
        {
            return new int[] { DataPointsSample.GetLength(0), DataPointsSample.GetLength(1)};
        }
    }
}
