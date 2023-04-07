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
        private int a, b;
        public TerrainDataHeightmap(string file_name)
        {
            Heightmap = LoadHeightmap(file_name);
            InputDataPoints = CreatePoints();

            Initialize();
        }

        public byte[,] LoadHeightmap(string file_name)
        {
            //Image je abstraktna trieda
            //Bitmapa je podtrieda Image
            //Bitmapa umoznuje pristup k jednotlivym pixelom, Image nie.

            //nacitanie obrazku je zo zdroja: https://www.codeproject.com/Articles/9727/Image-Processing-Lab-in-C

            Bitmap map = (Bitmap)System.Drawing.Image.FromFile(file_name);   //bitmapa je Color[,]


            //m = map.Width; n = map.Height;

            /*
            int start_width = 7 * map.Width / 8;
            int end_width = 15 * map.Width / 16;
            int start_height = 13 * map.Height / 32;
            int end_height = 15 * map.Height / 32;
            */
            int start_width = 0;
            int end_width = map.Width / 3;
            int start_height = 0;
            int end_height = map.Height / 3;

            a = end_width - start_width;
            b = end_height - start_height;


            byte[,] H = new byte[a, b]; //heightmapa

            for (int j = 0; j < b; j++)
            {
                for (int i = 0; i < a; i++)
                {

                    System.Drawing.Color color = map.GetPixel(start_width + i, start_height + j);
                    //obraz je sedotonovy, stanci sa pozriet bez ujmy na vseobecnost na 1 farbeny udaj z RGB (nie alfa)
                    byte h = color.R; //hodnota vysky, byty su od 0 po 255
                    H[i, j] = h;
                    //Console.WriteLine(color + " ");
                }
            }

            return H;
        }

        private Vector4[,] CreatePoints()
        {

            //idem tu vela prenasobovat, aby som dostala realne cisla udajov (v metroch)
            //zmenu hodnot na metre mozno mozem obist, aby som sa vyhla stratam presnosti na numerickych vypctoch
            //ale! pomer x a z treba zachovat, pretoze to bude dolezite pri detekcii spicov, hrebenov; kvoli stupaniu


            Vector4[,] Points = new Vector4[a, b]; //to-be IterpolatedPoints

            for (int j = 0; j < b; j++)
            {
                for (int i = 0; i < a; i++)
                {

                    //ked x1 po x2 su vzdialene o 1, predtavuje to 30 m
                    //z1 od z2 ak su vzidalene o 


                    float h = Heightmap[i, j];
                    //transformacia intervalov stary x patri [a,b] na novy y patri [c,d]; y=c+(x-a)*(d-c)/(b-a);          
                    //double z = Heightmap[i, j] * (max_height - min_height) / (255 - 0) + min_height;          
                    float z = h;// * (100 -0) / (255 - 0) ;   //zatial to preskaluvavam do stvorca 100^3
                                //Points[i, j] = new Vector3(i*samplingSize, j*samplingSize,z );
                    float w = 1.0f; //zaciatocna vaha 

                    Points[i, j] = new Vector4(i, j, z, w);
                }
            }

            return Points;
        }

    }
}
