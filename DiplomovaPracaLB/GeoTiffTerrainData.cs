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

    public class GeoTiffTerrainData : TerrainData
    {
        public GeoTiffTerrainData(typInterpolacie _typInterpolacie, int LOD, string file_name, int num_of_tiles_x, int num_of_tiles_y)
        {

            InputDataPoints = GeoTiffDataFromCSV(file_name, num_of_tiles_x, num_of_tiles_y);
            Inicialize(_typInterpolacie, LOD);
        }

        private Vector3[,] GeoTiffDataFromCSV(string file_name, int num_of_tiles_x, int num_of_tiles_y)
        {
            //TODO: SPRAVNE NACITANIE DAT. ODKIAL CERPAT DATA TAL, ABY BOLI PROPORCIE ZHODNE S REALITOU (a nie svtorcovy podorys, nech su vysky nad morom spravne v metroch)
            Vector3[,] LD = new Vector3[num_of_tiles_x + 1, num_of_tiles_y + 1];
            StreamReader streamReader = new StreamReader(file_name);  //ma sa nachadzat v bin/Debug

            string line = streamReader.ReadLine();  //prvy riadok nechcem, ak upravim kod matlabu, tak to mozem vymazat. obasuje "xyz"

            line = streamReader.ReadLine(); //az druhy riadok obsahuje suradnice
                                            //while (line != null)  //z
            for (int j = 0; j < num_of_tiles_y + 1; j++)
            {
                for (int i = 0; i < num_of_tiles_x + 1; i++)
                {
                    //line.Trim();    //spredu a zozadu odoberie WhiteSpace
                    string[] coordinates = line.Split(',');     //rozdeli string podla ciraky na slova

                    //msg.MessageBox.Show(coordinates[0]+" " +coordinates[1]+" "+ coordinates[2]);
                    if (coordinates.Length > 0)
                    {

                        float x = float.Parse(coordinates[0]) * 10000;
                        float y = float.Parse(coordinates[1]) * 10000;
                        float z = float.Parse(coordinates[2]);

                        //x a y nie su metre, ale uhly treba ich prenasobit. lol

                        //Console.WriteLine(x + " " + y + " " + z);

                        Vector3 pointCoordinates = new Vector3(x, y, z);

                        LD[i, j] = pointCoordinates;
                    }
                    else
                    {
                        msg.MessageBox.Show("snazi sa nacitat prazdny riadok");
                    }

                    line = streamReader.ReadLine();
                }
            }

            return LD;
        }

    }

}
