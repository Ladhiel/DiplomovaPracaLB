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

namespace DiplomovaPracaLB
{
    public class TerrainDataHeightmap : TerrainData
    {
        private byte[,] Heightmap;
        public TerrainDataHeightmap(int input_density, string file_name)
        {
            Heightmap = LoadHeightmap(file_name);
            DataPointsAll = CreatePoints();

            Initialize(input_density);
        }

        private byte[,] LoadHeightmap(string file_name)
        {
            //Image je abstraktna trieda
            //Bitmapa je podtrieda Image
            //Bitmapa umoznuje pristup k jednotlivym pixelom, Image nie.

            //nacitanie obrazku je zo zdroja: https://www.codeproject.com/Articles/9727/Image-Processing-Lab-in-C

            Bitmap map = (Bitmap)System.Drawing.Image.FromFile(file_name);   //bitmapa je Color[,]

            byte[,] H = new byte[map.Width, map.Height]; //heightmapa
            for (int j = 0; j < H.GetLength(1); j++)
            {
                for (int i = 0; i < H.GetLength(0); i++)
                {
                    H[i, j] = map.GetPixel(i, j).R;//obraz je sedotonovy, hodnota vysky, byty su od 0 po 255
                }
            }

            return H;
        }

        private Vector4[,] CreatePoints()
        {
            //idem tu vela prenasobovat, aby som dostala realne cisla udajov (v metroch)
            //zmenu hodnot na metre mozno mozem obist, aby som sa vyhla stratam presnosti na numerickych vypctoch
            //ale! pomer x a z treba zachovat, pretoze to bude dolezite pri detekcii spicov, hrebenov; kvoli stupaniu


            Vector4[,] Points = new Vector4[Heightmap.GetLength(0), Heightmap.GetLength(1)]; //to-be IterpolatedPoints
            for (int j = 0; j < Heightmap.GetLength(1); j++)
            {
                for (int i = 0; i < Heightmap.GetLength(0); i++)
                {
                    //transformacia intervalov stary x patri [a,b] na novy y patri [c,d]; y=c+(x-a)*(d-c)/(b-a);          
                    //double z = Heightmap[i, j] * (max_height - min_height) / (255 - 0) + min_height;          
                    float z = Heightmap[i, j];// * (100 -0) / (255 - 0) ;   //zatial to preskaluvavam do stvorca 100^3
                                              //Points[i, j] = new Vector3(i*samplingSize, j*samplingSize,z );
                    Points[i, j] = new Vector4(i * 3, j * 3, z, 1.0f); //tie trojky potom nahradit realnou hornodtou vzdialenosti
                }
            }

            return Points;
        }
    }
}
