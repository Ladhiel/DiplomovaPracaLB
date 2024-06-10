using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomovaPracaLB
{
    public class TerrainDataXYZ : TerrainData
    {
        public TerrainDataXYZ(int input_density, string file_name, int max_pixel_count)
        {
            DataPointsAll = XYZIntoGridVer2(file_name, max_pixel_count);
            Initialize(input_density);
        }

        private Vector4[,] XYZIntoGridVer2(string file_name, int max_pixel_count)
        {
            //vysledne UTM suradnice budu cele posunute, ale pomery vydialenosti budu spravne

            string[] lines = File.ReadAllLines(file_name);

            List<Vector4> FirstRowOfPoints = new List<Vector4>();

            float temp_y = float.MinValue;
            int dimX = 0, dimY = 0;
            bool y_klesa = false;   //idealne aby narastaal. ak opacne, musim prehodit kvoli normalam

            for (int i = 0; i < lines.Length; i++)   //to sa niekde breakne
            {
                lines[i].Trim();                                //spredu a zozadu odoberie WhiteSpace
                string[] coordinates = lines[i].Split(' ');     //rozdeli string podla ciraky na slova

                TestNumberDecimalSeparator(coordinates[0]);

                float y = float.Parse(coordinates[1]);

                if (temp_y != y)
                {
                    if (temp_y != float.MinValue)
                    {
                        float preistotu = (float)lines.Length / dimX;
                        dimY = (int)(preistotu);

                        //zistim ci y klesa alebo stupa
                        if (temp_y > y)
                        {
                            y_klesa = true;
                        }

                        break;
                    }
                    temp_y = y;
                }

                dimX++;
            }

            int i_max = dimX;
            int j_max = dimY;
            if (0 < max_pixel_count)
            {
                if (dimX > max_pixel_count)
                {
                    i_max = max_pixel_count;
                }

                if (dimY > max_pixel_count)
                {
                    j_max = max_pixel_count;
                }
            }

            Vector4[,] LD = new Vector4[i_max, j_max];
            for (int i = 0; i < i_max; i++)
            {
                for (int j = 0; j < j_max; j++)
                {
                    int index = i + j * dimX;

                    lines[index].Trim();    //spredu a zozadu odoberie WhiteSpace
                    string[] coordinates = lines[index].Split(' ');     //rozdeli string podla ciraky na slova

                    if (coordinates.Length > 0)
                    {
                        TestNumberDecimalSeparator(coordinates[0]);

                        double x = double.Parse(coordinates[0]);
                        double y = double.Parse(coordinates[1]);
                        double z = double.Parse(coordinates[2]) * 3;
                        float w = 1.0f;

                        Console.WriteLine(x + " " + y + " " + z);

                        if (y_klesa)
                        {
                            LD[i, j_max - j - 1] = new Vector4((float)x, (float)y, (float)z, w);
                        }
                        else
                        {
                            LD[i, j] = new Vector4((float)x, (float)y, (float)z, w);
                        }
                    }
                    else
                    {
                        //msg.MessageBox.Show("snazi sa nacitat prazdny riadok");
                    }
                }
            }

            return LD;
        }

        private bool TestNumberDecimalSeparator(string lineOfText)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();

            float parsed;
            try
            {
                parsed = float.Parse(lineOfText);
                return true;
            }
            catch
            {
                customCulture.NumberFormat.NumberDecimalSeparator = ".";
                System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

                try
                {
                    parsed = float.Parse(lineOfText);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
