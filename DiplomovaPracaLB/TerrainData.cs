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
        protected Vector4[,] OriginalDataPoints;    //cely dataset bodov
        public Vector4[,] InputDataPointsOriginal;  //body na vstupe - z ODP vybraty kadzny k-ty podla dentsity, s povodnymi vahami
        public Vector4[,] InputDataPoints;          //body na vstupe - z ODP vybraty kadzny k-ty podla dentsity
        
        private Splajn Interpolation;
        public Vector3 posunutie;
        public Matrix3 skalovanie;
    
        private int density = 2;  //hustota podmnoziny datasetu    
        private int[] border = new int[2];   //hranicne indexy pre porovnavaciu mriezku

        protected void Initialize()
        {
            //OriginalData su uz nacitane
            InputDataPoints = SelectSampleFromOrigData(density);
            SkopirujUdaje(InputDataPoints, ref InputDataPointsOriginal);
            FindExtremalCoordinates();
        }

            private Vector4[,] SelectSampleFromOrigData(int dens)
        {
            //zapamatam ohranicenie mriezky, z kt. vyberam sample
            int a = (OriginalDataPoints.GetLength(0) - 1) / dens; //vyuzivam celociselne delenie
            int b = (OriginalDataPoints.GetLength(1) - 1) / dens;

            border[0] = dens * a;   
            border[1] = dens * b;

            Vector4[,] IDP = new Vector4[a + 1, b + 1];

            for (int i = 0; i * dens < OriginalDataPoints.GetLength(0); i++)
            {
                for (int j = 0; j * dens < OriginalDataPoints.GetLength(1); j++)
                {
                    IDP[i, j] = OriginalDataPoints[i * dens, j * dens];
                }
            }

            return IDP;
        }

        public void ReInterpolate(int new_LOD)
        {
            Interpolation.AdjustLOD(InputDataPoints, new_LOD);
        }

        public void ReInterpolate(int active_m_index, int active_n_index, float new_weight)
        {
            InputDataPoints[active_m_index, active_n_index].X *= new_weight;
            InputDataPoints[active_m_index, active_n_index].Y *= new_weight;
            InputDataPoints[active_m_index, active_n_index].Z *= new_weight;
            InputDataPoints[active_m_index, active_n_index].W = new_weight;
            Interpolation.New(InputDataPoints);
        }

        public void UseKardBilin(float tenstion, int LOD)
        {
            Interpolation = new SplajnKardinalnyBilinearny(InputDataPoints, LOD, tenstion);
        }

        public void UseKardBicubic(float tenstion, int LOD)
        {
            Interpolation = new SplajnKardinalnyBikubicky(InputDataPoints, LOD, tenstion);
        }

        public void UseKochanekBartels(float tenstion, float continuity, float bias, int LOD)
        {
            Interpolation = new KochanekBartelsSplajn(InputDataPoints, LOD, tenstion, continuity, bias);
        }

        public void ResetAllWeights()
        {
            SkopirujUdaje(InputDataPointsOriginal, ref InputDataPoints);    //nacitaju sa povodne
            Interpolation.New(InputDataPoints);
        }

        public void ResetThisWeight(int i, int j)
        {
            InputDataPoints[i, j] = new Vector4(InputDataPointsOriginal[i, j]);
            Interpolation.New(InputDataPoints);
        }

        private void SkopirujUdaje(Vector4[,] Odkial, ref Vector4[,] Kam)
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

        //-----Getters------------------------------------------------------------------

        public Vector4[,] GetInterpolationPoints()
        {
            return Interpolation.InterpolationPoints;
        }

        public Vector3[,] GetNormals()
        {
            return Interpolation.Normals;
        }

    }
}
