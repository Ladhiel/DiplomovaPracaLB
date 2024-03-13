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
        public TerrainDataGeoTiff(string file_name, int num_of_tiles_x, int num_of_tiles_y)
        {
            DataPointsAll = GeoTiffDataFromCSV(file_name, num_of_tiles_x, num_of_tiles_y);
            Initialize();
        }

        public TerrainDataGeoTiff(string file_name)
        {
            DataPointsAll = GeoTiffDataFromGeotiff(file_name);
            Initialize();
        }

        private Vector4[,] GeoTiffDataFromGeotiff(string file_name)
        {
            Gdal.AllRegister();
            Dataset geotiffdata = Gdal.Open(file_name, Access.GA_ReadOnly);

            if (geotiffdata == null)
            {
                Console.WriteLine("Can't open " + file_name);
                return null;
            }

            //GEOTIFF INFO------------
            Console.WriteLine(geotiffdata.GetDriver().GetDescription());
            Console.WriteLine(geotiffdata.GetSpatialRef().__str__());   //projetion info in text form
            Console.WriteLine(geotiffdata.GetSpatialRef().GetUTMZone());

            double corner_UTM_x = 0, corner_UTM_y = 0, pixelSize_UTM_x = 0, pixelSize_UTM_y = 0;    //UTM, teda udaje pre plochy teren. Geotif je originalne na "sferoide"
            //File location info
            {
                //precitanie udajov
                double corner_geo_x, corner_geo_y, pixelSize_geo_x, pixelSize_geo_y;    //x = longitude, y= latitude
                {
                    double[] adfGeoTransform = new double[6];       //directly from GDAL documentation
                    geotiffdata.GetGeoTransform(adfGeoTransform);

                    //upper left raster corner coordinates in units from SpatialRef
                    corner_geo_x = adfGeoTransform[0];
                    corner_geo_y = adfGeoTransform[3];

                    //pixel size in units from SpatialRef
                    pixelSize_geo_x = adfGeoTransform[1];
                    pixelSize_geo_y = -adfGeoTransform[5]; //z konvencie to hodnota -dy. treba davat pozor v ktorej zemepisnej dlzke sme. Slovensko je napravo od 0°, tak beriem +dy

                    Console.WriteLine("tif file size:" + geotiffdata.RasterXSize + "x" + geotiffdata.RasterYSize);
                    Console.WriteLine(("Origin = (%.6f,%.6f)", adfGeoTransform[0], adfGeoTransform[3]));
                    Console.WriteLine(("Pixel Size = (%.6f,%.6f)", adfGeoTransform[1], adfGeoTransform[5]));

                }

                //preved z lat long na UTM
                {
                    //ESRI su kody pre suradnicove systemy pouzivane pre mapy, je ich asi 20 000.
                    //WKT je Well Known Text - popis transformacie suradnic

                    Console.WriteLine(geotiffdata.GetProjection());
                    Console.WriteLine(geotiffdata.GetSpatialRef().GetType());
                    Console.WriteLine(geotiffdata.GetSpatialRef().GetName());
                    Console.WriteLine(geotiffdata.GetSpatialRef());
                    Console.WriteLine(geotiffdata.GetSpatialRef().GetAxisName("GEOGCS", 0));
                    Console.WriteLine(geotiffdata.GetSpatialRef().GetAxisName("GEOGCS", 1));
                    int isNorth = (int)geotiffdata.GetSpatialRef().GetAxisOrientation("GEOGCS", 0);
                    Console.WriteLine(geotiffdata.GetSpatialRef().GetAxisName("GEOGCS", 1));


                    SpatialReference geoESRI = new SpatialReference("");
                    geoESRI.SetWellKnownGeogCS("WGS84");
                    SpatialReference flatESRI = new SpatialReference("");
                    int is_north = (int)geotiffdata.GetSpatialRef().GetAxisOrientation("GEOGCS", 0);  //returns enum AxisOrientation. "1" is North.
                    int UTMzone = (int)Math.Floor((corner_geo_x + 180) / 6) + 1;         //najdi casovu zonu (poludniky)
                    flatESRI.SetUTM(UTMzone, is_north);

                    OSGeo.OGR.Geometry point1 = new OSGeo.OGR.Geometry(wkbGeometryType.wkbPoint);
                    OSGeo.OGR.Geometry point2 = new OSGeo.OGR.Geometry(wkbGeometryType.wkbPoint);
                    point1.AddPoint(corner_geo_x, corner_geo_y, 1);    // na z nezalezi      uppper left
                    point2.AddPoint(corner_geo_x + pixelSize_geo_x * geotiffdata.RasterXSize, corner_geo_y - pixelSize_geo_y * geotiffdata.RasterYSize, 1);   //lower right
                    point1.AssignSpatialReference(geoESRI);
                    point2.AssignSpatialReference(geoESRI);
                    point1.TransformTo(flatESRI);//konverzia suradnic
                    point2.TransformTo(flatESRI);

                    double[] outUTM = { 0, 0 };

                    point1.GetPoint(0, outUTM);
                    corner_UTM_x = outUTM[0];
                    corner_UTM_y = outUTM[1];

                    point2.GetPoint(0, outUTM);
                    double opposite_UTM_x = outUTM[0];
                    double opposite_UTM_y = outUTM[1];

                    pixelSize_UTM_x = (opposite_UTM_x - corner_UTM_x) / geotiffdata.RasterXSize;
                    pixelSize_UTM_y = (opposite_UTM_y - corner_UTM_y) / geotiffdata.RasterYSize;
                }
            }

            OSGeo.GDAL.Band rasterBand = geotiffdata.GetRasterBand(1);

            int rasterXSize=0, rasterYSize=0;
            GetRasterBandBasicInfo(ref rasterBand, out rasterXSize, out rasterYSize);


            //dataset points:
            float[] hodnoty_test = new float[rasterXSize * rasterYSize];    // drzi precitane hodnoty
            rasterBand.ReadRaster(0, 0, rasterXSize, rasterYSize, hodnoty_test, rasterXSize, rasterYSize, 0, 0);    //cita vyskove hodnoty

            Vector4[,] LD = new Vector4[rasterXSize, rasterYSize];  //drzi si body datasetu
            double x = 0, y = 0, z = 0; //temp coord values
            
            for (int j = 0; j < rasterYSize; j++)
            {
                y = corner_UTM_y + j * pixelSize_UTM_y;

                for (int i = 0; i < rasterXSize; i++)
                {
                    x = corner_UTM_x - i * pixelSize_UTM_x;
                    z = hodnoty_test[i* rasterXSize+j];

                    LD[i, j] = new Vector4((float)x/10, (float)y/10, (float)z, 1.0f);  
                    //Console.WriteLine("["+x+"\t"+y+ "\t" + z +"]");
                }
            }


            //TODO transform

        



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

                        //x a y nie su metre, ale uhly treba ich prenasobit. lol

                        //Console.WriteLine(x + " " + y + " " + z);

                        Vector4 pointCoordinates = new Vector4(x, y, z, w);

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
