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

namespace DiplomovaPracaLB
{
    public class TerrainDataMatlab : TerrainData
    {
        private int a, b;
        public TerrainDataMatlab(string file_name, int hmap_num_patterns)
        {
            a = hmap_num_patterns + 1;
            b = hmap_num_patterns + 1;
            DataPointsAll = MatlabDataIntoGrid(MatlabDataLoadText(file_name));

            Initialize();
        }

        private List<Vector4> MatlabDataLoadText(string file_name)
        {
            List<Vector4> LD = new List<Vector4>();
            StreamReader streamReader = new StreamReader(file_name);  //ma sa nachadzat v bin/Debug

            string line = streamReader.ReadLine();
            while (line != null)
            {
                line.Trim();    //spredu a zozadu odoberie WhiteSpace
                string[] coordinates = line.Split(' ');     //rozdeli string podla medzier na slova

                //msg.MessageBox.Show(coordinates[0]+" " +coordinates[1]+" "+ coordinates[2]);
                if (coordinates.Length > 0)
                {
                    float x = float.Parse(coordinates[0]);
                    float y = float.Parse(coordinates[1]);
                    float z = float.Parse(coordinates[2]);
                    float w = 1.0f;

                    Vector4 pointCoordinates = new Vector4(x, y, z, w);

                    LD.Add(pointCoordinates);
                }
                else
                {
                    //msg.MessageBox.Show("snazi sa nacitat prazdny riadok");
                }
                line = streamReader.ReadLine();
            }
            return LD;
        }

        private Vector4[,] MatlabDataIntoGrid(List<Vector4> LD)
        {
            //Vieme, ze matlab text subor ma 257*257 raidkov, kazdych 257 riadkov je jeden riadok v suradnici x pri pevnom y.
            Vector4[,] PlanarGrid = new Vector4[a, b];
            int k = 0;
            for (int j = 0; j < b; j++)
            {
                for (int i = 0; i < a; i++)
                {
                    PlanarGrid[i, j] = LD[k];
                    k++;
                }
            }

            return PlanarGrid;
        }
    }
}