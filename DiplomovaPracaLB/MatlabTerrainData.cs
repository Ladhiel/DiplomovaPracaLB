﻿using System;
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

    public class MatlabTerrainData : TerrainData
    {
        public MatlabTerrainData(typInterpolacie _typInterpolacie, int LOD, string file_name, int hmap_num_patterns)
        {
            m = hmap_num_patterns + 1;
            n = hmap_num_patterns + 1;
            InputDataPoints = MatlabDataIntoGrid(MatlabDataLoadText(file_name));

            Inicialize(_typInterpolacie, LOD);
        }


        private List<Vector3> MatlabDataLoadText(string file_name)
        {
            List<Vector3> LD = new List<Vector3>();
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

                    Vector3 pointCoordinates = new Vector3(x, y, z);


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


        private Vector3[,] MatlabDataIntoGrid(List<Vector3> LD)
        {

            //Viem, ze matlab text subor ma 257*257 raidkov, kazdych 257 riadkov je jeden riadok v suradnici x pri pevnom y.
            Vector3[,] PlanarGrid = new Vector3[m, n];
            int k = 0;
            for (int j = 0; j < n; j++)
            {
                for (int i = 0; i < m; i++)
                {
                    PlanarGrid[i, j] = LD[k];
                    k++;
                }
            }

            return PlanarGrid;
        }
    }
}