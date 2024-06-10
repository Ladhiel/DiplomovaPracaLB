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
using OSGeo.GDAL;
using System.Data;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using OSGeo.OSR;
using OSGeo.OGR;

namespace DiplomovaPracaLB
{
    public class TerrainDataGeoTiff : TerrainData
    {
        public TerrainDataGeoTiff(int input_density, string file_name, int x_max_pixel_count = 0, int y_max_pixel_count = 0)
        {
            DataPointsAll = GeoTiffDataFromGeotiff(file_name, x_max_pixel_count, y_max_pixel_count);
            Initialize(input_density);
        }

        private Vector4[,] GeoTiffDataFromGeotiff(string file_name, int x_max_pixel_count = 0, int y_max_pixel_count = 0)
        {

            Gdal.AllRegister();
            Dataset geotiffdata = Gdal.Open(file_name, Access.GA_ReadOnly);

            if (geotiffdata == null)
            {
                Console.WriteLine("Can't open " + file_name);
                return null;
            }

            //viem interpretovat iba udaje "WGS 84"
            if (geotiffdata.GetSpatialRef().GetName() != "WGS 84")
            {
                Console.WriteLine("Can't read " + geotiffdata.GetSpatialRef().GetName() + " spatial reference, only WGS 84.");
                return null;
            }

            //GEOTIFF INFO------------
            Console.WriteLine("Typ suboru: " + geotiffdata.GetDriver().GetDescription());
            Console.WriteLine("Info o projekcii: \n" + geotiffdata.GetSpatialRef().__str__());   //projetion info in text form

            string outS = "";
            string[] s = geotiffdata.GetMetadata(outS);
            foreach (string s2 in s)
            {
                Console.WriteLine("line " + s2);
            }

            //code follows GDAL documentation:

            //precitanie udajov
            double[] adfGeoTransform = new double[6];
            geotiffdata.GetGeoTransform(adfGeoTransform);

            //upper left raster corner coordinates in units from SpatialRef
            double corner_geo_lat = adfGeoTransform[3];
            double corner_geo_long = adfGeoTransform[0];

            //pixel size in units from SpatialRef
            double pixelSize_geo_lat = adfGeoTransform[1];
            double pixelSize_geo_long = adfGeoTransform[5]; //z konvencie to hodnota -dy

            Console.WriteLine("tif file size:" + geotiffdata.RasterXSize + "x" + geotiffdata.RasterYSize);
            Console.WriteLine(("Origin = (%.6f,%.6f)", corner_geo_lat, corner_geo_long));
            Console.WriteLine(("Pixel Size = (%.6f,%.6f)", pixelSize_geo_lat, pixelSize_geo_long));


            //preved z lat long na UTM
            CoordinateTransformation CoordTransform;
            {
                //ESRI eviduje kody pre suradnicove systemy pouzivane pre mapy, je ich asi 20 000.
                //WKT je Well Known Text - popis transformacie suradnic

                SpatialReference geoESRI = geotiffdata.GetSpatialRef();              //geoESRI.SetWellKnownGeogCS("WGS84");

                int ESPGcode = MakeEspgCode(corner_geo_lat, corner_geo_long);
                SpatialReference flatESRI = new SpatialReference("");
                try
                {
                    flatESRI.ImportFromEPSG(ESPGcode);
                }
                catch (ApplicationException e)
                {
                    Console.WriteLine("Wasn't able to find an EPSG code");
                    return null;
                }

                WriteoutAbout(geoESRI);
                WriteoutAbout(flatESRI);

                CoordTransform = new CoordinateTransformation(geoESRI, flatESRI);
            }

            OSGeo.GDAL.Band rasterBand = geotiffdata.GetRasterBand(1);
            Console.WriteLine(rasterBand.GetUnitType());    //metres

            int rasterXSize, rasterYSize;
            GetRasterBandBasicInfo(ref rasterBand, out rasterXSize, out rasterYSize);

            //dataset points:
            float[] vyskove_hodnoty = new float[rasterXSize * rasterYSize];    //precitane hodnoty
            rasterBand.ReadRaster(0, 0, rasterXSize, rasterYSize, vyskove_hodnoty, rasterXSize, rasterYSize, 0, 0);    //cita vyskove hodnoty


            int i_max = rasterXSize;
            int j_max = rasterYSize;
            if (0 < x_max_pixel_count || x_max_pixel_count < rasterXSize)
            {
                i_max = x_max_pixel_count;
            }
            if (0 < y_max_pixel_count || y_max_pixel_count < rasterYSize)
            {
                j_max = y_max_pixel_count;
            }


            Vector4[,] LD = new Vector4[i_max, j_max];  //body datasetu
            double[] coord = new double[3];
            for (int i = 0; i < i_max; i++)
            {
                for (int j = 0; j < j_max; j++)
                {
                    coord[0] = corner_geo_lat + i * pixelSize_geo_lat;
                    coord[1] = corner_geo_long + j * pixelSize_geo_long;    //y
                    coord[2] = vyskove_hodnoty[i + j * rasterXSize];        //z

                    CoordTransform.TransformPoint(coord);

                    LD[i, j_max - j - 1] = new Vector4((float)coord[0], (float)coord[1], (float)coord[2], 1.0f);
                    Console.WriteLine("[" + coord[0] + "\t" + coord[1] + "\t" + coord[2] + "]");
                }
            }

            geotiffdata.Dispose();
            return LD;
        }

        private void GetRasterBandBasicInfo(ref OSGeo.GDAL.Band rasterBand, out int rasterXSize, out int rasterYSize)
        {
            double[] minmax_elevation;
            rasterXSize = rasterBand.XSize;
            rasterYSize = rasterBand.YSize;

            // rasterBand.GetBlockSize(out rasterXSize, out rasterYSize);
            Console.WriteLine("Block=" + rasterXSize + "x" + rasterYSize);
            Console.WriteLine("Type=" + rasterBand.DataType.ToString());
            Console.WriteLine("ColorInterp=" + rasterBand.GetColorInterpretation().ToString());

            //gdal doumentation says to do this, in case file is not well defined, but its not neessary, you can do ....
            double[] minmax_val = { 0, 0 }; //16bit raster -32768,32767 
            int[] minmax_exists = { 0, 0 };
            rasterBand.GetMinimum(out minmax_val[0], out minmax_exists[0]);
            rasterBand.GetMaximum(out minmax_val[1], out minmax_exists[1]);
            Console.WriteLine("Min=" + minmax_val[0] + "Max=" + minmax_val[1]);

            minmax_elevation = new double[2] { 0, 0 };
            int approx_ok = 1;  //smth to do with statisics
            if (!(Convert.ToBoolean(minmax_exists[0]) && Convert.ToBoolean(minmax_exists[1])))
            {
                rasterBand.ComputeRasterMinMax(minmax_elevation, approx_ok); //..... just this is sufficient to read min max elevation
                //GDALComputeRasterMinMax((GDALRasterBandH)poBand, true, adfMinMax);   //c++
            }
            Console.WriteLine("MinElev=" + minmax_elevation[0] + "MaxElev=" + minmax_elevation[1]);
            if (rasterBand.GetOverviewCount() > 0)
            {
                Console.WriteLine("Band has %d overviews.", rasterBand.GetOverviewCount());
            }
        }

        private void WriteoutAbout(SpatialReference SpatialRef)
        {
            Console.WriteLine(SpatialRef);

            Console.WriteLine(
                "WGS 84 ako snimane: "
                + SpatialRef.GetName());

            Console.WriteLine(
                "Kod suradnicovej sustavy: "
                + SpatialRef.GetAuthorityName("GEOGCS") + ":"
                + SpatialRef.GetAuthorityCode("GEOGCS"));

            Console.WriteLine(SpatialRef.GetAxisName("GEOGCS", 0));
            Console.WriteLine(SpatialRef.GetAxisName("GEOGCS", 1));
            Console.WriteLine();
            Console.WriteLine(SpatialRef.__str__());   //projetion info in text form

            Console.WriteLine("--------------------------------------------");
        }

        private int MakeEspgCode(double latitude, double longitude)
        {
            int ESPGcode;
            //How ESPG codes are made for WGS 84:

            string ESPGtext;
            if (latitude < 0)
            {
                ESPGtext = "327";    //south
            }
            else
            {
                ESPGtext = "326";    //north
            }

            int UTMzone = (int)Math.Floor((longitude + 180) / 6) + 1;
            ESPGtext += UTMzone.ToString();

            if (int.TryParse(ESPGtext, out ESPGcode))
            {
                return ESPGcode;
            }

            return 0;
        }

#if false
        private Vector4[,] GeoTiffDataFromCSV(string file_name, int num_of_tiles_x, int num_of_tiles_y)
        {
            //TODO: SPRAVNE NACITANIE DAT. ODKIAL CERPAT DATA TAL, ABY BOLI PROPORCIE ZHODNE S REALITOU (a nie svtorcovy podorys, nech su vysky nad morom spravne v metroch)
            Vector4[,] LD = new Vector4[num_of_tiles_x + 1, num_of_tiles_y + 1];
            StreamReader streamReader = new StreamReader(file_name);  //ma sa nachadzat v bin/Debug

            streamReader.ReadLine();  //prvy riadok nechcem, ak upravim kod matlabu, tak to mozem vymazat. obasuje "xyz"

            string line = streamReader.ReadLine(); //az druhy riadok obsahuje suradnice
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
                        float w = 1.0f; //zaciatocna vaha je 1

                        //x a y nie su metre, ale uhly treba ich prenasobit.

                        //Console.WriteLine(x + " " + y + " " + z);

                        Vector4 pointCoordinates = new Vector4(x, y, z, w);

                        LD[i, j] = pointCoordinates;
                    }
                    else
                    {
                        //msg.MessageBox.Show("snazi sa nacitat prazdny riadok");
                    }

                    line = streamReader.ReadLine();
                }
            }

            return LD;
        }
#endif
    }
}
