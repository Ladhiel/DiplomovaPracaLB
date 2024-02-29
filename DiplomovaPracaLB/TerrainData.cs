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
        public Vector4[,] DataPointsSample;  //body na vstupe - z ODP vybraty kadzny k-ty podla dentsity, s povodnymi vahami
        public Vector4[,] WeightedDataPointsSample;          //body na vstupe - z ODP vybraty kadzny k-ty podla dentsity

        //private Splajn Interpolation;
        public Vector3 posunutie;
        public Matrix3 skalovanie;

        private int density = 1;  //hustota podmnoziny datasetu    
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

        //-----Getters------------------------------------------------------------------

        public void SetDensity(int new_density)
        {
            if(0<=new_density && new_density <=20);
            {
                density = new_density;
            }

        }
    }
}
