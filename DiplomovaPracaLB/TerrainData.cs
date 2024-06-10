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
using g3;
using static OpenTK.Graphics.OpenGL.GL;
using System.Drawing;
using System.Windows.Media.Animation;


namespace DiplomovaPracaLB
{
    public abstract class TerrainData
    {
        public Vector4[,] DataPointsAll;               //cely dataset bodov
        public Vector4[,] DataPointsSample;            //body na vstupe - z ODP vybraty kadzny k-ty podla dentsity, s povodnymi vahami
        public Vector4[,] WeightedDataPointsSample;    //body na vstupe - z ODP vybraty kadzny k-ty podla dentsity

        public Vector[,] TempEvalPoints;

        //private Splajn Interpolation;
        public Vector3 posunutie;
        public Matrix3 skalovanie;

        private int density = 1;  //hustota vyberu podmnoziny datasetu. Vybera sa kazdy (density). bod
        private float min_z, max_z, min_x, max_x, min_y, max_y;
        private double average_of_minimal_distances, approx_enclosing_sphere_diameter; //useful for RBF


        public DMesh3 MeshDataAll;

        protected void Initialize(int input_density)
        {
            //OriginalData su uz nacitane

            SetDensity(input_density);
            WeightedDataPointsSample = SelectSampleFromOrigData();
            ZalohujSample();

            //vypocitanie uzitocnych hodnot
            FindMinMax();
            MeshDataAll = CreateMesh(ref DataPointsAll);
            average_of_minimal_distances = FindAverageMinimalDistances(ref DataPointsSample);
            approx_enclosing_sphere_diameter = FindApproxEnclosingDiameter(ref DataPointsSample);
        }

        private Vector4[,] SelectSampleFromOrigData()
        {
            //zapamatam ohranicenie mriezky, z kt. vyberam sample
            int a = (DataPointsAll.GetLength(0) - 1) / density; //vyuzivam celociselne delenie
            int b = (DataPointsAll.GetLength(1) - 1) / density;

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

        protected void FindMinMax()
        {
            float span_x, span_y, span_z, mid_x, mid_y, mid_z;

            min_x = float.MaxValue;
            max_x = float.MinValue;
            min_y = float.MaxValue;
            max_y = float.MinValue;
            min_z = float.MaxValue;
            max_z = float.MinValue;


            for (int j = 0; j < DataPointsSample.GetLength(1); j++)
            {
                for (int i = 0; i < DataPointsSample.GetLength(0); i++)
                {
                    //minmax
                    float x = DataPointsSample[i, j].X;
                    float y = DataPointsSample[i, j].Y;
                    float z = DataPointsSample[i, j].Z;

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

        private double FindAverageMinimalDistances(ref Vector4[,] PointGrid)
        {
            int total_num = PointGrid.GetLength(0) * PointGrid.GetLength(1);

            double sum_of_minimal_distances = 0;

            for (int i = 0; i < PointGrid.GetLength(0); i++)
            {
                for (int j = 0; j < PointGrid.GetLength(1); j++)
                {
                    double min_Distance = double.MaxValue;

                    double[] distance = new double[4];

                    //vyuzijem ze som v cca pravidelnej mriezke - najblizsi je niektory zo susedov
                    distance[1] = (i > 0) ? (PointGrid[i, j] - PointGrid[i - 1, j]).Length : double.MaxValue;
                    distance[3] = (j > 0) ? (PointGrid[i, j] - PointGrid[i, j - 1]).Length : double.MaxValue;
                    distance[0] = (i < PointGrid.GetLength(0) - 1) ? (PointGrid[i, j] - PointGrid[i + 1, j]).Length : double.MaxValue;
                    distance[2] = (j < PointGrid.GetLength(1) - 1) ? (PointGrid[i, j] - PointGrid[i, j + 1]).Length : double.MaxValue;

                    for (int k = 0; k < 4; k++)
                    {
                        if (min_Distance > distance[k]) min_Distance = distance[k];
                    }

                    sum_of_minimal_distances += min_Distance;
                }
            }

            return sum_of_minimal_distances / total_num;
        }

        private double FindApproxEnclosingDiameter(ref Vector4[,] PointGrid)
        {
            //Polomer je minimalne dlhsia diagonala mriezky.
            //Rozsah nadmorskych vysok realneho terenu je casto zanedbatelna voci jeho rozlohe.

            double diag0011 = (PointGrid[0, 0] - PointGrid[PointGrid.GetLength(0) - 1, PointGrid.GetLength(1) - 1]).Length;
            double diag0110 = (PointGrid[0, PointGrid.GetLength(1) - 1] - PointGrid[PointGrid.GetLength(0) - 1, 0]).Length;

            return (diag0011 > diag0110) ? diag0011 : diag0110;
        }

        private g3.DMesh3 CreateMesh(ref Vector4[,] PointGrid)
        {
            //Official g3Sharp tutorial: https://www.gradientspace.com/tutorials/2017/7/20/basic-mesh-creation-with-g3sharp

            DMesh3 DataMesh = new DMesh3(MeshComponents.All);

            int i_max = PointGrid.GetLength(0);
            int j_max = PointGrid.GetLength(1);

            //Add vertices to mesh
            for (int i = 0; i < i_max; i++)
            {
                for (int j = 0; j < j_max; j++)
                {
                    // [i, j] --> [i * j_max + j]
                    DataMesh.AppendVertex(new g3.Vector3f(PointGrid[i, j].X, PointGrid[i, j].Y, PointGrid[i, j].Z));
                }
            }

            //Make mesh from datagrid
            int vi00, vi01, vi10, vi11;
            for (int i = 0; i < i_max - 1; i++)
            {
                for (int j = 0; j < j_max - 1; j++)
                {
                    //vrcholy priesotorveho stvoruholnika maju indexy:
                    vi00 = i * j_max + j;
                    vi01 = i * j_max + j + 1;
                    vi10 = (i + 1) * j_max + j;
                    vi11 = (i + 1) * j_max + j + 1;

                    double diag0011 = (PointGrid[i, j] - PointGrid[i + 1, j + 1]).Length;
                    double diag1001 = (PointGrid[i + 1, j] - PointGrid[i, j + 1]).Length;

                    if (diag0011 < diag1001) //Kratsia diagonala predeluje stvoruholnik na dva trojuholniky (standardny postup)
                    {
                        DataMesh.AppendTriangle(vi00, vi01, vi11);
                        DataMesh.AppendTriangle(vi00, vi11, vi10);
                        /*
                            v00 ---- v01
                              | \     |
                              |   \   |
                              |     \ |
                            v10 ---- v11
                        */
                    }
                    else
                    {
                        DataMesh.AppendTriangle(vi00, vi01, vi10);
                        DataMesh.AppendTriangle(vi01, vi11, vi10);
                        /*
                            v00 ---- v01
                              |     / |
                              |   /   |
                              | /     |
                            v10 ---- v11
                        */
                    }
                }
            }

            return DataMesh;
        }

        //-----Getters------------------------------------------------------------------

        public float GetMinMaxVal(bool false_is_min_and_true_is_max, int axis_index)
        {
            if (false_is_min_and_true_is_max)
            {
                if (axis_index == 0) return max_x;
                if (axis_index == 1) return max_y;
                return max_z;
            }
            if (axis_index == 0) return min_x;
            if (axis_index == 1) return min_y;
            return min_z;
        }

        public void SetDensity(int new_density)
        {
            if (0 <= new_density && new_density < DataPointsAll.GetLength(0) && new_density < DataPointsAll.GetLength(1))
            {
                density = new_density;
            }
        }

        public int GetDensity()
        {
            return density;
        }

        public int[] GetSampleSize()
        {
            return new int[] { DataPointsSample.GetLength(0), DataPointsSample.GetLength(1) };
        }

        public double GetAverageMinimalDistanceOfSample()
        {
            return average_of_minimal_distances;
        }

        public double GetApproximateDiameterOfSample()
        {
            return approx_enclosing_sphere_diameter;
        }
    }
}
