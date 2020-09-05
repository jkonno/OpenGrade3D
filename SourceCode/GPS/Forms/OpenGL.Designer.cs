﻿
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SharpGL;

namespace OpenGrade
{
    public partial class FormGPS
    {
        //extracted Near, Far, Right, Left clipping planes of frustum
        public double[] frustum = new double[24];

        //difference between blade tip and guide line
        public double cutDelta;
        private double minDist;
        private double minDistMap;

        //public bool stopTheProgram;
        

        private double minDistMapDist = 400; // how far from a survey point it will draw the map 400 is 20 meters
        private double drawPtWidth = 1; // the size of the map pixel in meter

        //the point in the real world made from clicked screen coords
        vec2 screen2FieldPt = new vec2();

        double fovy = 45;
        double camDistanceFactor = -2;
        int mouseX = 0, mouseY = 0;

        //data buffer for pixels read from off screen buffer
        byte[] grnPixels = new byte[80001];

        /// Handles the OpenGLDraw event of the openGLControl control.
        private void openGLControl_OpenGLDraw(object sender, RenderEventArgs e)
        {
            if (isGPSPositionInitialized)
            {

                //  Get the OpenGL object.
                OpenGL gl = openGLControl.OpenGL;
                //System.Threading.Thread.Sleep(500);

                //  Clear the color and depth buffer.
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                gl.LoadIdentity();

                //camera does translations and rotations
                camera.SetWorldCam(gl, pn.easting, pn.northing, camHeading);

                //draw the field ground images
                worldGrid.DrawFieldSurface();

                ////Draw the world grid based on camera position
                gl.Disable(OpenGL.GL_DEPTH_TEST);
                gl.Disable(OpenGL.GL_TEXTURE_2D);


                gl.Enable(OpenGL.GL_LINE_SMOOTH);
                gl.Enable(OpenGL.GL_BLEND);

                gl.Hint(OpenGL.GL_LINE_SMOOTH_HINT, OpenGL.GL_FASTEST);
                gl.Hint(OpenGL.GL_POINT_SMOOTH_HINT, OpenGL.GL_FASTEST);
                gl.Hint(OpenGL.GL_POLYGON_SMOOTH_HINT, OpenGL.GL_FASTEST);

                ////if grid is on draw it
                if (isGridOn) worldGrid.DrawWorldGrid(gridZoom);

                //turn on blend for paths
                gl.Enable(OpenGL.GL_BLEND);

                //section patch color
                gl.Color(redSections, grnSections, bluSections, (byte)160);
                if (isDrawPolygons) gl.PolygonMode(OpenGL.GL_FRONT, OpenGL.GL_LINE);

                gl.PolygonMode(OpenGL.GL_FRONT, OpenGL.GL_FILL);
                gl.Color(1, 1, 1);

                //draw contour line if button on 
                //if (ct.isContourBtnOn)


                // draw the current and reference AB Lines
                if (ABLine.isABLineSet | ABLine.isABLineBeingSet) ABLine.DrawABLines();
                else ct.DrawContourLine();

                //draw the flags if there are some
                int flagCnt = flagPts.Count;
                if (flagCnt > 0)
                {
                    for (int f = 0; f < flagCnt; f++)
                    {
                        gl.PointSize(8.0f);
                        gl.Begin(OpenGL.GL_POINTS);
                        if (flagPts[f].color == 0) gl.Color((byte)255, (byte)0, (byte)flagPts[f].ID);
                        if (flagPts[f].color == 1) gl.Color((byte)0, (byte)255, (byte)flagPts[f].ID);
                        if (flagPts[f].color == 2) gl.Color((byte)255, (byte)255, (byte)flagPts[f].ID);
                        gl.Vertex(flagPts[f].easting, flagPts[f].northing, 0);
                        gl.End();
                    }

                    if (flagNumberPicked != 0)
                    {
                        ////draw the box around flag
                        gl.LineWidth(4);
                        gl.Color(0.980f, 0.0f, 0.980f);
                        gl.Begin(OpenGL.GL_LINE_STRIP);

                        double offSet = (zoomValue * zoomValue * 0.01);
                        gl.Vertex(flagPts[flagNumberPicked - 1].easting, flagPts[flagNumberPicked - 1].northing + offSet, 0);
                        gl.Vertex(flagPts[flagNumberPicked - 1].easting - offSet, flagPts[flagNumberPicked - 1].northing, 0);
                        gl.Vertex(flagPts[flagNumberPicked - 1].easting, flagPts[flagNumberPicked - 1].northing - offSet, 0);
                        gl.Vertex(flagPts[flagNumberPicked - 1].easting + offSet, flagPts[flagNumberPicked - 1].northing, 0);
                        gl.Vertex(flagPts[flagNumberPicked - 1].easting, flagPts[flagNumberPicked - 1].northing + offSet, 0);

                        gl.End();

                        //draw the flag with a black dot inside
                        gl.PointSize(4.0f);
                        gl.Color(0, 0, 0);
                        gl.Begin(OpenGL.GL_POINTS);
                        gl.Vertex(flagPts[flagNumberPicked - 1].easting, flagPts[flagNumberPicked - 1].northing, 0);
                        gl.End();
                    }
                }

                //screen text for debug
                //gl.DrawText(120, 10, 1, 1, 1, "Courier Bold", 18, "Head: " + saveCounter.ToString("N1"));
                //gl.DrawText(120, 40, 1, 1, 1, "Courier Bold", 18, "Tool: " + distTool.ToString("N1"));
                //gl.DrawText(120, 70, 1, 1, 1, "Courier Bold", 18, "Where: " + yt.whereAmI.ToString());
                //gl.DrawText(120, 100, 1, 1, 1, "Courier Bold", 18, "Seq: " + yt.isSequenceTriggered.ToString());
                //gl.DrawText(120, 40, 1, 1, 1, "Courier Bold", 18, "  GPS: " + Convert.ToString(Math.Round(glm.toDegrees(gpsHeading), 2)));
                //gl.DrawText(120, 70, 1, 1, 1, "Courier Bold", 18, "Fixed: " + Convert.ToString(Math.Round(glm.toDegrees(gyroCorrected), 2)));
                //gl.DrawText(120, 100, 1, 1, 1, "Courier Bold", 18, "L/Min: " + Convert.ToString(rc.CalculateRateLitersPerMinute()));
                //gl.DrawText(120, 130, 1, 1, 1, "Courier", 18, "       Roll: " + Convert.ToString(glm.toDegrees(rollDistance)));
                //gl.DrawText(120, 160, 1, 1, 1, "Courier", 18, "       Turn: " + Convert.ToString(Math.Round(turnDelta, 4)));
                //gl.DrawText(40, 120, 1, 0.5, 1, "Courier", 12, " frame msec " + Convert.ToString((int)(frameTime)));

                //draw the vehicle/implement
                vehicle.DrawVehicle();

                //Back to normal
                gl.Color(0.98f, 0.98f, 0.98f);
                gl.Disable(OpenGL.GL_BLEND);
                gl.Enable(OpenGL.GL_DEPTH_TEST);

                //// 2D Ortho --------------------------
                gl.MatrixMode(OpenGL.GL_PROJECTION);
                gl.PushMatrix();
                gl.LoadIdentity();

                //negative and positive on width, 0 at top to bottom ortho view
                gl.Ortho2D(-(double)Width / 2, (double)Width / 2, (double)Height, 0);

                //  Create the appropriate modelview matrix.
                gl.MatrixMode(OpenGL.GL_MODELVIEW);
                gl.PushMatrix();
                gl.LoadIdentity();

                if (isSkyOn)
                {
                    ////draw the background when in 3D
                    if (camera.camPitch < -60)
                    {
                        //-10 to -32 (top) is camera pitch range. Set skybox to line up with horizon 
                        double hite = (camera.camPitch + 60) / -20 * 0.34;
                        //hite = 0.001;

                        //the background
                        double winLeftPos = -(double)Width / 2;
                        double winRightPos = -winLeftPos;
                        gl.Enable(OpenGL.GL_TEXTURE_2D);
                        gl.BindTexture(OpenGL.GL_TEXTURE_2D, texture[0]);		// Select Our Texture

                        gl.Begin(OpenGL.GL_TRIANGLE_STRIP);				// Build Quad From A Triangle Strip
                        gl.TexCoord(0, 0); gl.Vertex(winRightPos, 0.0); // Top Right
                        gl.TexCoord(1, 0); gl.Vertex(winLeftPos, 0.0); // Top Left
                        gl.TexCoord(0, 1); gl.Vertex(winRightPos, hite * (double)Height); // Bottom Right
                        gl.TexCoord(1, 1); gl.Vertex(winLeftPos, hite * (double)Height); // Bottom Left
                        gl.End();						// Done Building Triangle Strip

                        //disable, straight color
                        gl.Disable(OpenGL.GL_TEXTURE_2D);
                    }
                }

                //LightBar if AB Line is set and turned on or contour
                if (isLightbarOn)
                {
                    if (ct.isContourBtnOn)
                    {
                        string dist;
                        txtDistanceOffABLine.Visible = true;
                        //lblDelta.Visible = true;
                        if (ct.distanceFromCurrentLine == 32000) ct.distanceFromCurrentLine = 0;

                        DrawLightBar(openGLControl.Width, openGLControl.Height, ct.distanceFromCurrentLine * 0.1);
                        if ((ct.distanceFromCurrentLine) < 0.0)
                        {
                            txtDistanceOffABLine.ForeColor = Color.Green;
                            if (isMetric) dist = ((int)Math.Abs(ct.distanceFromCurrentLine * 0.1)) + " ->";
                            else dist = ((int)Math.Abs(ct.distanceFromCurrentLine / 2.54 * 0.1)) + " ->";
                            txtDistanceOffABLine.Text = dist;
                        }

                        else
                        {
                            txtDistanceOffABLine.ForeColor = Color.Red;
                            if (isMetric) dist = "<- " + ((int)Math.Abs(ct.distanceFromCurrentLine * 0.1));
                            else dist = "<- " + ((int)Math.Abs(ct.distanceFromCurrentLine / 2.54 * 0.1));
                            txtDistanceOffABLine.Text = dist;
                        }

                        //if (guidanceLineHeadingDelta < 0) lblDelta.ForeColor = Color.Red;
                        //else lblDelta.ForeColor = Color.Green;

                        //if (guidanceLineDistanceOff == 32020 | guidanceLineDistanceOff == 32000) btnAutoSteer.Text = "-";
                        //else btnAutoSteer.Text = "Y";
                    }

                    else
                    {
                        if (ABLine.isABLineSet | ABLine.isABLineBeingSet)
                        {
                            string dist;

                            txtDistanceOffABLine.Visible = true;
                            //lblDelta.Visible = true;
                            DrawLightBar(openGLControl.Width, openGLControl.Height, ABLine.distanceFromCurrentLine * 0.1);
                            if ((ABLine.distanceFromCurrentLine) < 0.0)
                            {
                                // --->
                                txtDistanceOffABLine.ForeColor = Color.Green;
                                if (isMetric) dist = ((int)Math.Abs(ABLine.distanceFromCurrentLine * 0.1)) + " ->";
                                else dist = ((int)Math.Abs(ABLine.distanceFromCurrentLine / 2.54 * 0.1)) + " ->";
                                txtDistanceOffABLine.Text = dist;
                            }

                            else
                            {
                                // <----
                                txtDistanceOffABLine.ForeColor = Color.Red;
                                if (isMetric) dist = "<- " + ((int)Math.Abs(ABLine.distanceFromCurrentLine * 0.1));
                                else dist = "<- " + ((int)Math.Abs(ABLine.distanceFromCurrentLine / 2.54 * 0.1));
                                txtDistanceOffABLine.Text = dist;
                            }

                            //if (guidanceLineHeadingDelta < 0) lblDelta.ForeColor = Color.Red;
                            //else lblDelta.ForeColor = Color.Green;
                            //if (guidanceLineDistanceOff == 32020 | guidanceLineDistanceOff == 32000) btnAutoSteer.Text = "-";
                            //else btnAutoSteer.Text = "Y";
                        }
                    }

                    //AB line is not set so turn off numbers
                    if (!ABLine.isABLineSet & !ABLine.isABLineBeingSet & !ct.isContourBtnOn)
                    {
                        txtDistanceOffABLine.Visible = false;
                        //btnAutoSteer.Text = "-";
                    }
                }
                else
                {
                    txtDistanceOffABLine.Visible = false;
                    //btnAutoSteer.Text = "-";
                }

                gl.Flush();//finish openGL commands
                gl.PopMatrix();//  Pop the modelview.

                //  back to the projection and pop it, then back to the model view.
                gl.MatrixMode(OpenGL.GL_PROJECTION);
                gl.PopMatrix();
                gl.MatrixMode(OpenGL.GL_MODELVIEW);

                //reset point size
                gl.PointSize(1.0f);
                gl.Flush();

                if (leftMouseDownOnOpenGL)
                {
                    leftMouseDownOnOpenGL = false;
                    byte[] data1 = new byte[192];

                    //scan the center of click and a set of square points around
                    gl.ReadPixels(mouseX - 4, mouseY - 4, 8, 8, OpenGL.GL_RGB, OpenGL.GL_UNSIGNED_BYTE, data1);

                    //made it here so no flag found
                    flagNumberPicked = 0;

                    for (int ctr = 0; ctr < 192; ctr += 3)
                    {
                        if (data1[ctr] == 255 | data1[ctr + 1] == 255)
                        {
                            flagNumberPicked = data1[ctr + 2];
                            break;
                        }
                    }
                }


                //digital input Master control (WorkSwitch)
                if (isJobStarted && mc.isWorkSwitchEnabled)
                {
                    //check condition of work switch
                    if (mc.isWorkSwitchActiveLow)
                    {
                        //if (mc.workSwitchValue == 0)
                    }
                    else
                    {
                        //if (mc.workSwitchValue == 1)
                    }
                }

                //stop the timer and calc how long it took to do calcs and draw
                frameTime = (double)swFrame.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency * 1000;

                //if a couple minute has elapsed save the field in case of crash and to be able to resume            
                if (saveCounter > 180)       //2 counts per second X 60 seconds = 120 counts per minute. , now 5 count per sec so 300 per minute
                {
                    if (isJobStarted && stripOnlineGPS.Value != 1)
                    {
                        //auto save the field patches, contours accumulated so far
                        FileSaveField();
                        FileSaveMapPt();
                        FileSaveContour();
                        

                        //NMEA log file
                        if (isLogNMEA) FileSaveNMEA();
                    }
                    saveCounter = 0;
                }

                openGLControlBack.DoRender();
            }
        }

        /// Handles the OpenGLInitialized event of the openGLControl control.
        private void openGLControl_OpenGLInitialized(object sender, EventArgs e)
        {
            //  Get the OpenGL object.
            OpenGL gl = openGLControl.OpenGL;

            //Load all the textures
            LoadGLTextures();

            //  Set the clear color.
            gl.ClearColor(0.22f, 0.2858f, 0.16f, 1.0f);

            // Set The Blending Function For Translucency
            gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);

            //gl.Disable(OpenGL.GL_CULL_FACE);
            gl.CullFace(OpenGL.GL_BACK);

            //set the camera to right distance
            SetZoom();

            //now start the timer assuming no errors, otherwise the program will not stop on errors.
            tmrWatchdog.Enabled = true;
        }

        /// Handles the Resized event of the openGLControl control.
        private void openGLControl_Resized(object sender, EventArgs e)
        {
            //  Get the OpenGL object.
            OpenGL gl = openGLControl.OpenGL;

            //  Set the projection matrix.
            gl.MatrixMode(OpenGL.GL_PROJECTION);

            //  Load the identity.
            gl.LoadIdentity();

            //  Create a perspective transformation.
            gl.Perspective(fovy, (double)openGLControl.Width / (double)openGLControl.Height, 1, camDistanceFactor * camera.camSetDistance);

            //  Set the modelview matrix.
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
        }

        //main openGL draw function
        private void openGLControlBack_OpenGLDraw(object sender, RenderEventArgs args)
        {
            OpenGL gl = openGLControlBack.OpenGL;

            //antialiasing - fastest
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);  // Clear The Screen And The Depth Buffer

            gl.Enable(OpenGL.GL_LINE_SMOOTH);
            //gl.Enable(OpenGL.GL_BLEND);

            gl.Hint(OpenGL.GL_LINE_SMOOTH_HINT, OpenGL.GL_FASTEST);
            gl.Hint(OpenGL.GL_POINT_SMOOTH_HINT, OpenGL.GL_FASTEST);
            gl.Hint(OpenGL.GL_POLYGON_SMOOTH_HINT, OpenGL.GL_FASTEST);

            gl.LoadIdentity();                  // Reset The View

            // Fill the eleViewList
            


            //if adding new points recalc mins maxes
            //if (manualBtnState == btnStates.Rec) CalculateMinMaxZoom();

            //autogain the window
            if ((maxFieldY - minFieldY) != 0)
                altitudeWindowGain = (Math.Abs(cameraDistanceZ / (maxFieldY - minFieldY))) * 0.80;
            else altitudeWindowGain = 900;

            if (altitudeWindowGain > 900) altitudeWindowGain = 900;

            //translate to that spot in the world 
            gl.Translate(0, 0, -cameraDistanceZ);
            gl.Translate(-centerX, -centerY, 0);

            gl.Color(1, 1, 1);

            //reset cut delta for frame
            cutDelta = 9999;

            int closestPoint = 0;
            int ptCnt = ct.ptList.Count;
            gl.LineWidth(4);


            if (cboxLaserModeOnOff.Checked)
            {

                cutDelta = (pn.altitude - ct.zeroAltitude) * 100;

            }


            if (ptCnt > 0)
            {


                //to change for ptList whit 4 points avreage

                minDist = 1000000; //original is 1000000
                //int ptCount = ct.mapList.Count - 1;

                //find the closest point to current fix
                /*for (int t = 0; t < ptCount; t++)
                {
                    double dist = ((pn.easting - ct.mapList[t].eastingMap) * (pn.easting - ct.mapList[t].eastingMap))
                                    + ((pn.northing - ct.mapList[t].northingMap) * (pn.northing - ct.mapList[t].northingMap));
                    if (dist < minDist) { minDist = dist; closestPoint = t; }
                }
                */
                // end to change


                //int closestPointMap = 0;
                int closestPointMapSE = -1;
                int closestPointMapSW = -1;
                int closestPointMapNE = -1;
                int closestPointMapNW = -1;

                int ptCount = ct.ptList.Count - 1;
                
                double minDistSE = 900; // if the point is further than 30 meters we forget it
                double minDistSW = 900;
                double minDistNE = 900;
                double minDistNW = 900;

                //find the closest point to current fix
                for (int t = 0; t < ptCount; t++)
                {
                    double distMap = ((pn.easting - ct.ptList[t].easting) * (pn.easting - ct.ptList[t].easting))
                                    + ((pn.northing - ct.ptList[t].northing) * (pn.northing - ct.ptList[t].northing));
                    if (distMap < minDist)
                    {
                        minDist = distMap;
                        closestPoint = t;
                    }

                    //Search closest point South West
                    if (pn.easting >= ct.ptList[t].easting && pn.northing >= ct.ptList[t].northing)
                    {
                       
                        if (distMap < minDistSW)
                        {
                            minDistSW = distMap;
                            closestPointMapSW = t;
                        }
                    }

                    //Search closest point South East
                    if (pn.easting <= ct.ptList[t].easting && pn.northing >= ct.ptList[t].northing)
                    {
                        
                        if (distMap < minDistSE)
                        {
                            minDistSE = distMap;
                            closestPointMapSE = t;
                        }
                    }

                    //Search closest point North West
                    if (pn.easting >= ct.ptList[t].easting && pn.northing <= ct.ptList[t].northing)
                    {
                       
                        if (distMap < minDistNW)
                        {
                            minDistNW = distMap;
                            closestPointMapNW = t;
                        }
                    }

                    //Search closest point North East
                    if (pn.easting <= ct.ptList[t].easting && pn.northing <= ct.ptList[t].northing)
                    {
                        
                        if (distMap < minDistNE)
                        {
                            minDistNE = distMap;
                            closestPointMapNE = t;
                        }
                    }


                }

                // Calulate the closest point alitude and cutAltitude

                //double cutFillMap;
                double avgAltitude = -1; 
                double avgCutAltitude = -1;

                ct.usedPtList.Clear();
                UsedPt point = new UsedPt(ct.ptList[closestPoint].easting, ct.ptList[closestPoint].northing, 1);
                ct.usedPtList.Add(point);


                if (minDist < ((vehicle.toolWidth/3)* (vehicle.toolWidth / 3)) | minDistSE == 900 | minDistSW == 900 | minDistNE == 900 | minDistNW == 900)
                {
                    // if the closest point is under the center of the blade or there is not a point in each direction
                    avgAltitude = ct.ptList[closestPoint].altitude;
                    avgCutAltitude = ct.ptList[closestPoint].cutAltitude;
                }
                else
                {
                    //if (closestPointMapNE > -1 && closestPointMapSE > -1 && closestPointMapNW > -1 && closestPointMapSW > -1)
                    {
                        //if there is a point in eash 4 directions
                        double sumofCloseDist = 1 / Math.Sqrt(minDistSE) + 1 / Math.Sqrt(minDistSW) + 1 / Math.Sqrt(minDistNW) + 1 / Math.Sqrt(minDistNE);

                        avgAltitude = ((ct.ptList[closestPointMapNE].altitude / Math.Sqrt(minDistNE)) + (ct.ptList[closestPointMapNW].altitude / Math.Sqrt(minDistNW)) +
                        (ct.ptList[closestPointMapSE].altitude / Math.Sqrt(minDistSE)) + (ct.ptList[closestPointMapSW].altitude / Math.Sqrt(minDistSW))) / sumofCloseDist;

                        if (ct.ptList[closestPointMapNE].cutAltitude == -1 | ct.ptList[closestPointMapNW].cutAltitude == -1 | ct.ptList[closestPointMapSE].cutAltitude == -1 | ct.ptList[closestPointMapSW].cutAltitude == -1)
                        {
                            avgCutAltitude = ct.ptList[closestPoint].cutAltitude;
                        }
                        else
                        {
                            avgCutAltitude = ((ct.ptList[closestPointMapNE].cutAltitude / Math.Sqrt(minDistNE)) + (ct.ptList[closestPointMapNW].cutAltitude / Math.Sqrt(minDistNW)) +
                        (ct.ptList[closestPointMapSE].cutAltitude / Math.Sqrt(minDistSE)) + (ct.ptList[closestPointMapSW].cutAltitude / Math.Sqrt(minDistSW))) / sumofCloseDist;

                        }

                        // build the list to view the points in the map
                        UsedPt point2 = new UsedPt(ct.ptList[closestPointMapNE].easting, ct.ptList[closestPointMapNE].northing, 1);
                        ct.usedPtList.Add(point2);

                        UsedPt point3 = new UsedPt(ct.ptList[closestPointMapNW].easting, ct.ptList[closestPointMapNW].northing, 1);
                        ct.usedPtList.Add(point3);

                        UsedPt point4 = new UsedPt(ct.ptList[closestPointMapSE].easting, ct.ptList[closestPointMapSE].northing, 1);
                        ct.usedPtList.Add(point4);

                        UsedPt point5 = new UsedPt(ct.ptList[closestPointMapSW].easting, ct.ptList[closestPointMapSW].northing, 1);
                        ct.usedPtList.Add(point5);

                    }
                    //else
                    //{
                        // if there are not 4 points check for 2 points in opposite side
                        
                       // not need for this?
                    //}

                }
                //if (avgCutAltitude == -1) cutFillMap = 9999;
                //else cutFillMap = avgCutAltitude - avgAltitude;


                if (!ct.surveyMode && ct.eleViewList.Count > 0)
                {
                    //fill in the latest distance and fix
                    double fixDist = ((ct.eleViewList[101].easting - pn.easting) * (ct.eleViewList[101].easting - pn.easting) + (ct.eleViewList[101].northing - pn.northing) * (ct.eleViewList[101].northing - pn.northing));
                    if (fixDist > 0.25)
                    {

                        //copy each point one count back: 0 take 1, 1 take 2 etc.
                        
                        for (int i = 0; i < 101; i++)
                        {


                            ct.eleViewList[i].lastPassAltitude = ct.eleViewList[i + 1].lastPassAltitude;
                            ct.eleViewList[i].easting = ct.eleViewList[i + 1].easting;
                            ct.eleViewList[i].northing = ct.eleViewList[i + 1].northing;
                            ct.eleViewList[i].altitude = ct.eleViewList[i + 1].altitude;
                            ct.eleViewList[i].cutAltitude = ct.eleViewList[i + 1].cutAltitude;
                            
                        }
                        
                        //for (int i = 0; i < 101; i++) ct.eleViewList[i] = ct.eleViewList[i + 1]; this is not working

                        // fill the current point (101)
                        ct.eleViewList[101].lastPassAltitude = pn.altitude;
                        ct.eleViewList[101].easting = pn.easting;
                        ct.eleViewList[101].northing = pn.northing;

                        if (minDist < 900)
                        {
                            ct.eleViewList[101].altitude = avgAltitude;
                            ct.eleViewList[101].cutAltitude = avgCutAltitude;
                        }
                        else
                        {
                            ct.eleViewList[101].altitude = -1;
                            ct.eleViewList[101].cutAltitude = -1;
                        }

                        // make the look ahead view
                        // 200 points to fill, 102 to 298? 4 * 49 = 196

                        for (int j = 1; j < 50; j++)
                        {

                            double AheadEasting = pn.easting + Math.Cos(fixHeading + glm.PIBy2) * -2*j;
                            double AheadNorthing = pn.northing + Math.Sin(fixHeading - glm.PIBy2) * -2*j;


                            double mindist = 1000000;
                            int ClosestLookAheadPt = 999999;
                            int lookPtCt = ct.ptList.Count;

                            if (lookPtCt > 0)
                            {
                                for (int m = 0; m < lookPtCt; m++)
                                {
                                    double distA = (AheadEasting - ct.ptList[m].easting) * (AheadEasting - ct.ptList[m].easting) + 
                                        (AheadNorthing - ct.ptList[m].northing) * (AheadNorthing - ct.ptList[m].northing);

                                    if (distA < mindist)
                                    {
                                        mindist = distA;
                                        ClosestLookAheadPt = m;
                                    }
                                }
                            }







                            for (int k = 0; k < 4; k++)
                            {
                                ct.eleViewList[101 + j * 4 - k].easting = AheadEasting;
                                ct.eleViewList[101 + j * 4 - k].northing = AheadNorthing;

                                if (ClosestLookAheadPt != 999999 && mindist < 100)
                                {
                                    //ct.eleViewList[101 + j * 10 - k].lastPassAltitude = pn.altitude;
                                    ct.eleViewList[101 + j * 4 - k].altitude = ct.ptList[ClosestLookAheadPt].altitude;
                                    ct.eleViewList[101 + j * 4 - k].cutAltitude = ct.ptList[ClosestLookAheadPt].cutAltitude;


                                }
                                else
                                {
                                    //ct.eleViewList[101 + j * 10 - k].lastPassAltitude = pn.altitude;
                                    ct.eleViewList[101 + j * 4 - k].altitude = -1;
                                    ct.eleViewList[101 + j * 4 - k].cutAltitude = -1;

                                }

                            }
                        }








                        CalculateMinMaxZoom();
                    }



                }




                // Change to eleViewList
                //draw the ground profile

                int elePtCount = ct.eleViewList.Count;
                if (elePtCount > 100)
                {


                    gl.Color(0.32f, 0.32f, 0.32f);
                    gl.Begin(OpenGL.GL_TRIANGLE_STRIP);
                    for (int i = 0; i < elePtCount; i++)
                    {
                        gl.Vertex(i,
                          (((ct.eleViewList[i].altitude - centerY) * altitudeWindowGain) + centerY), 0);
                        gl.Vertex(i, -10000, 0);
                    }
                    gl.End();

                    //cut line drawn in full
                    //int cutPts = ct.mapList.Count;
                    //if (cutPts > 0)
                    //{
                    
                        
                    
                    
                    gl.Color(0.974f, 0.0f, 0.12f);
                    gl.Begin(OpenGL.GL_LINE_STRIP);
                    for (int i = 0; i < elePtCount; i++)
                    {
                        if (ct.eleViewList[i].cutAltitude > 0)
                            gl.Vertex(i, (((ct.eleViewList[i].cutAltitude - centerY) * altitudeWindowGain) + centerY), 0);
                        else
                        {
                            gl.End();
                            gl.Begin(OpenGL.GL_LINE_STRIP);
                        }
                    }
                    gl.End();
                    //}

                    //crosshairs same spot as mouse - long
                    gl.LineWidth(2);
                    gl.Enable(OpenGL.GL_LINE_STIPPLE);
                    gl.LineStipple(1, 0x0300);

                    gl.Begin(OpenGL.GL_LINES);
                    gl.Color(0.90f, 0.90f, 0.70f);
                    gl.Vertex(screen2FieldPt.easting, 3000, 0);
                    gl.Vertex(screen2FieldPt.easting, -3000, 0);
                    gl.Vertex(-10, (((screen2FieldPt.northing - centerY) * altitudeWindowGain) + centerY), 0);
                    gl.Vertex(1000, (((screen2FieldPt.northing - centerY) * altitudeWindowGain) + centerY), 0);
                    gl.End();
                    gl.Disable(OpenGL.GL_LINE_STIPPLE);

                    //draw last pass if rec on
                    //if (cboxRecLastOnOff.Checked & ct.ptList[closestPoint].cutAltitude > 0)
                    //ct.ptList[closestPoint].lastPassAltitude = pn.altitude;

                    //draw if on
                    //if (cboxLastPass.Checked)
                    //{
                        gl.LineWidth(2);
                        gl.Begin(OpenGL.GL_LINE_STRIP);

                        gl.Color(0.40f, 0.970f, 0.400f);
                        for (int i = 0; i < elePtCount; i++)
                        {
                            if (ct.eleViewList[i].lastPassAltitude > 0)
                                gl.Vertex(i, (((ct.eleViewList[i].lastPassAltitude - centerY) * altitudeWindowGain) + centerY), 0);
                        }
                        gl.End();
                    //}
                    //draw the actual elevation lines and blade
                    gl.LineWidth(8);
                    gl.Begin(OpenGL.GL_LINES);
                    gl.Color(0.95f, 0.90f, 0.0f);
                    gl.Vertex(101, (((pn.altitude - centerY) * altitudeWindowGain) + centerY), 0);
                    gl.Vertex(101, 10000, 0);
                    gl.End();

                    //the skinny actual elevation lines
                    gl.LineWidth(1);
                    gl.Begin(OpenGL.GL_LINES);
                    gl.Color(0.57f, 0.80f, 0.00f);
                    gl.Vertex(-5000, (((pn.altitude - centerY) * altitudeWindowGain) + centerY), 0);
                    gl.Vertex(5000, (((pn.altitude - centerY) * altitudeWindowGain) + centerY), 0);
                    gl.Vertex(101, -10000, 0);
                    gl.Vertex(101, 10000, 0);
                    gl.End();

                    //little point at cutting edge of blade
                    gl.Color(0.0f, 0.0f, 0.0f);
                    gl.PointSize(8);
                    gl.Begin(OpenGL.GL_POINTS);
                    gl.Vertex(101, (((pn.altitude - centerY) * altitudeWindowGain) + centerY), 0);
                    gl.End();

                }

                if (minDist < 900) // original is 15, meter form the line scare, for 5 meter put 25
                {
                       

                        //record last pass

                        ////draw last pass
                        //if (cboxLastPass.Checked)
                        //{
                        //    ct.ptList[closestPoint].lastPassAltitude = pn.altitude;
                        //    gl.LineWidth(2);
                        //    gl.Begin(OpenGL.GL_LINE_STRIP);

                        //    //the dashed accent of ground profile
                        //    gl.Color(0.40f, 0.970f, 0.400f);
                        //    for (int i = 0; i < ptCnt; i++)
                        //    {
                        //        if (ct.ptList[i].lastPassAltitude > 0)
                        //            gl.Vertex(i, (((ct.ptList[i].lastPassAltitude - centerY) * altitudeWindowGain) + centerY), 0);
                        //    }
                        //    gl.End();
                        //}

                        


                    

                    //calculate blade to guideline delta
                    //double temp = (double)closestPoint / (double)count2;
                    if (cboxLaserModeOnOff.Checked)
                    {

                        cutDelta = (pn.altitude - ct.zeroAltitude) * 100;

                    }
                    else
                    {
                        if (avgCutAltitude > 0)
                        {
                            //in cm
                            cutDelta = (pn.altitude - avgCutAltitude) * 100;
                        }
                    }
                }
            }



        }

        private void openGLControlBack_MouseMove(object sender, MouseEventArgs e)
        {
            Point screenPt = new Point();
            screenPt.X = e.Location.X;
            screenPt.Y = ((openGLControlBack.Height - e.Location.Y) - openGLControlBack.Height / 2);

            //convert screen coordinates to field coordinates
            screen2FieldPt.easting = ((double)screenPt.X) * (double)cameraDistanceZ / openGLControlBack.Width;
            screen2FieldPt.northing = ((double)screenPt.Y) * (double)cameraDistanceZ / (openGLControlBack.Height * altitudeWindowGain);
            screen2FieldPt.northing += centerY;

            if (maxFieldX == 0)
            {
                stripTopoLocation.Text = " 0 " + ": " + " 0.000" + ": " + " 0.0";
            }
            else
            {
                if (ct.ptList.Count > 0)
                {
                    if (isMetric) stripTopoLocation.Text = ((int)(screen2FieldPt.easting)).ToString() + ": " + screen2FieldPt.northing.ToString("N3") + ": " + ((screen2FieldPt.northing - ct.mapList[(int)(screen2FieldPt.easting)].altitudeMap) * 100).ToString("N1");
                    else stripTopoLocation.Text = ((int)(screen2FieldPt.easting)).ToString() + ": " + ((screen2FieldPt.northing) / .0254 / 12).ToString("N3") + ": " + ((screen2FieldPt.northing - ct.mapList[(int)(screen2FieldPt.easting)].altitudeMap) / .0254).ToString("N1");
                }
                else stripTopoLocation.Text = " 0 " + ": " + " 0.000" + ": " + " 0.0";
            }

            
        }

        private void openGLControlBack_MouseClick(object sender, MouseEventArgs e)
        {
            Point fixPt = new Point();
            vec2 plotPt = new vec2();
            
        }

        #region 3D mapping

        public double eastingMin, eastingMax, northingMin, northingMax;
        
        // determine ptList min and max easting and northing -by Pat
        public void CalculateMinMaxEastNort()
        {
            //stopTheProgram = true;

            eastingMin = 9999999;
            eastingMax = -9999999;
            northingMin = 9999999;
            northingMax = -9999999;

            drawPtWidth = Properties.Vehicle.Default.setVehicle_GradeDistFromLine;
            if (drawPtWidth < 1) drawPtWidth = 1;

            int cnt = ct.ptList.Count;

            if (cnt > 0)
            {
                for (int i = 0; i < cnt; i++)
                {
                    //double x = i;
                    double y = ct.ptList[i].easting;
                    double z = ct.ptList[i].northing;

                    //find min max coordonates
                    if (eastingMin > y) eastingMin = y;
                    if (eastingMax < y) eastingMax = y;
                    if (northingMin > z) northingMin = z;
                    if (northingMax < z) northingMax = z;
                }
            }

            if (eastingMax == -9999999 | eastingMin == 9999999 | northingMax == -9999999 | northingMin == 9999999)
            {
                eastingMin = 0; eastingMax = 0; northingMax = 0; northingMin = 0;
            }
            else
            {
                eastingMin -= minDistMapDist; eastingMax += minDistMapDist; northingMax += minDistMapDist; northingMin -= minDistMapDist;
            }

            int ptCnt = ct.ptList.Count;

            if (ptCnt > 0)
            {
                for (double h = (double)eastingMin; h < (double)eastingMax; h += drawPtWidth)
                {
                    for (double i = (double)northingMin; i < (double)northingMax; i += drawPtWidth)
                    {

                   
                        int closestPointMap = 0;
                        int closestPointMapSE = -1;
                        int closestPointMapSW = -1;
                        int closestPointMapNE = -1;
                        int closestPointMapNW = -1;

                        int ptCount = ct.ptList.Count - 1;
                        minDistMap = 1000;
                        double minDistSE = 1000;
                        double minDistSW = 1000;
                        double minDistNE = 1000;
                        double minDistNW = 1000;

                        //find the closest point to current fix
                        for (int t = 0; t < ptCount; t++)
                        {
                            double distMap = ((h - ct.ptList[t].easting) * (h - ct.ptList[t].easting))
                                            + ((i - ct.ptList[t].northing) * (i - ct.ptList[t].northing));
                            if (distMap < minDistMap)
                            {
                                minDistMap = distMap;
                                closestPointMap = t;       
                            }

                            //Search closest point South West
                            if (h >= ct.ptList[t].easting && i >= ct.ptList[t].northing)
                            {
                                //double distMapSW = ((h - ct.ptList[t].easting) * (h - ct.ptList[t].easting))
                                //            + ((i - ct.ptList[t].northing) * (i - ct.ptList[t].northing));
                                if (distMap < minDistSW)
                                {
                                    minDistSW = distMap;
                                    closestPointMapSW = t;
                                }
                            }

                            //Search closest point South East
                            if (h <= ct.ptList[t].easting && i >= ct.ptList[t].northing)
                            {
                                //double distMapSE = ((h - ct.ptList[t].easting) * (h - ct.ptList[t].easting))
                                //            + ((i - ct.ptList[t].northing) * (i - ct.ptList[t].northing));
                                if (distMap < minDistSE)
                                {
                                    minDistSE = distMap;
                                    closestPointMapSE = t;
                                }
                            }

                            //Search closest point North West
                            if (h >= ct.ptList[t].easting && i <= ct.ptList[t].northing)
                            {
                                //double distMapNW = ((h - ct.ptList[t].easting) * (h - ct.ptList[t].easting))
                                //            + ((i - ct.ptList[t].northing) * (i - ct.ptList[t].northing));
                                if (distMap < minDistNW)
                                {
                                    minDistNW = distMap;
                                    closestPointMapNW = t;
                                }
                            }

                            //Search closest point North East
                            if (h <= ct.ptList[t].easting && i <= ct.ptList[t].northing)
                            {
                                //double distMapNE = ((h - ct.ptList[t].easting) * (h - ct.ptList[t].easting))
                                //            + ((i - ct.ptList[t].northing) * (i - ct.ptList[t].northing));
                                if (distMap < minDistNE)
                                {
                                    minDistNE = distMap;
                                    closestPointMapNE = t;
                                }
                            }


                        }






                        if (minDistMap < minDistMapDist)
                        {
                            double cutFillMap;
                            double avgAltitude;
                            double avgCutAltitude;
                            double sumofCloseDist = 1 / Math.Sqrt(minDistSE) + 1 / Math.Sqrt(minDistSW) + 1 / Math.Sqrt(minDistNW) + 1 / Math.Sqrt(minDistNE);

                            if (minDistMap <= drawPtWidth | minDistSE == 1000 | minDistSW == 1000 | minDistNE == 1000 | minDistNW == 1000)
                            {
                                avgAltitude = ct.ptList[closestPointMap].altitude;
                                avgCutAltitude = ct.ptList[closestPointMap].cutAltitude;
                            }
                            else
                            {
                                avgAltitude = ((ct.ptList[closestPointMapNE].altitude / Math.Sqrt(minDistNE)) + (ct.ptList[closestPointMapNW].altitude / Math.Sqrt(minDistNW)) +
                                (ct.ptList[closestPointMapSE].altitude / Math.Sqrt(minDistSE)) + (ct.ptList[closestPointMapSW].altitude / Math.Sqrt(minDistSW))) / sumofCloseDist;

                                if (ct.ptList[closestPointMapNE].cutAltitude < 1 | ct.ptList[closestPointMapNW].cutAltitude < 1 | ct.ptList[closestPointMapSE].cutAltitude < 1 | ct.ptList[closestPointMapSW].cutAltitude < 1)
                                {
                                    avgCutAltitude = ct.ptList[closestPointMap].cutAltitude;
                                }
                                else
                                {
                                    avgCutAltitude = ((ct.ptList[closestPointMapNE].cutAltitude / Math.Sqrt(minDistNE)) + (ct.ptList[closestPointMapNW].cutAltitude / Math.Sqrt(minDistNW)) +
                                (ct.ptList[closestPointMapSE].cutAltitude / Math.Sqrt(minDistSE)) + (ct.ptList[closestPointMapSW].cutAltitude / Math.Sqrt(minDistSW))) / sumofCloseDist;

                                }


                            }

                           

                            if (avgCutAltitude < 1) cutFillMap = 9999;
                            else cutFillMap = avgCutAltitude - avgAltitude;

                            mapListPt point = new mapListPt(h, i, drawPtWidth, avgAltitude, avgCutAltitude,
                                cutFillMap, ct.ptList[closestPointMap].lastPassAltitude);
                            ct.mapList.Add(point);
                        }
                    }
                }
            }
            FileSaveMapPt(); // For keeping the visual mapping

            //stopTheProgram = false;
        }

        #endregion

        double maxFieldX, maxFieldY, minFieldX, minFieldY, centerX, centerY, cameraDistanceZ;

        //determine mins maxs of contour and altitude
        private void CalculateMinMaxZoom()
        {
            minFieldX = 0; minFieldY = 9999999;
            maxFieldX = 300; maxFieldY = -9999999;

            //every time the section turns off and on is a new patch
            int cnt = ct.eleViewList.Count;

            if (cnt > 0)
            {
                for (int i = 0; i < cnt; i++)
                {
                    //double x = i;
                    double y = ct.eleViewList[i].altitude;

                    //also tally the max/min of Cut x and z
                    //if (minFieldX > x) minFieldX = x;
                    //if (maxFieldX < x) maxFieldX = x;
                    if (y > 0)
                    {
                        if (minFieldY > y) minFieldY = y;
                    }
                    
                    
                    if (maxFieldY < y) maxFieldY = y;
                    
                    
                }  
                
                
            }

            if (maxFieldX == -9999999 | minFieldX == 9999999 | maxFieldY == -9999999 | minFieldY == 9999999)
            {
                maxFieldX = 0; minFieldX = 0; maxFieldY = 0; minFieldY = 0;
                cameraDistanceZ = 10;
            }
            else
            {
                //Max horizontal
                cameraDistanceZ = Math.Abs(minFieldX - maxFieldX);

                if (cameraDistanceZ < 10) cameraDistanceZ = 10;
                if (cameraDistanceZ > 6000) cameraDistanceZ = 6000;

                maxFieldY = (maxFieldY + vehicle.viewDistAboveGnd);
                minFieldY = (minFieldY - vehicle.viewDistUnderGnd);


                centerX = (maxFieldX + minFieldX) / 2.0;
                centerY = ((maxFieldY) + (minFieldY)) / 2.0;
                if (isMetric) stripMinMax.Text=(minFieldY + vehicle.viewDistUnderGnd).ToString("N2") + ":" + (maxFieldY - vehicle.viewDistAboveGnd).ToString("N2");
                else stripMinMax.Text = ((minFieldY + vehicle.viewDistUnderGnd)/.0254/12).ToString("N2") + ":" + ((maxFieldY - vehicle.viewDistAboveGnd)/.0254/12).ToString("N2");
            }
        }

        
       

       
        
        /*
        private void CalculateContourPointDistances()
        {
            int cnt = ct.ptList.Count;

            if (cnt > 0)
            {
                ct.ptList[0].distance = 0;
                for (int i = 0; i < cnt-1; i++)
                {
                    ct.ptList[i + 1].distance = pn.Distance(ct.ptList[i].northing, ct.ptList[i].easting,ct.ptList[i + 1].northing, ct.ptList[i + 1].easting);
                }
            }
        }
        */

        //Draw section OpenGL window, not visible
        private void openGLControlBack_OpenGLInitialized(object sender, EventArgs e)
        {
            //LoadGLTexturesBack();
            OpenGL gls = openGLControlBack.OpenGL;

            //  Set the clear color.
            gls.ClearColor(0.5f, 0.5f, 0.5f, 1.0f);

            // Set The Blending Function For Translucency
            gls.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);

            gls.Enable(OpenGL.GL_BLEND);
            //gls.Disable(OpenGL.GL_DEPTH_TEST);

            gls.Enable(OpenGL.GL_CULL_FACE);
            gls.CullFace(OpenGL.GL_BACK);
        }

       //Resize is called upn window creation
        private void openGLControlBack_Resized(object sender, EventArgs e)
        {
            //  Get the OpenGL object.
            OpenGL gls = openGLControlBack.OpenGL;

            gls.MatrixMode(OpenGL.GL_PROJECTION);

            //  Load the identity.
            gls.LoadIdentity();

            // change these at your own peril!!!! Very critical
            //  Create a perspective transformation.
            gls.Perspective(53.1, 1, 1, 6000);

            //  Set the modelview matrix.
            gls.MatrixMode(OpenGL.GL_MODELVIEW);
        }

        //done in ortho mode
        public void DrawLightBar(double Width, double Height, double offlineDistance)
        {
            //  Get the OpenGL object.
            OpenGL gl = openGLControl.OpenGL;
            double down = 20;

            gl.LineWidth(1);
            
            //  Dot distance is representation of how far from AB Line
            int dotDistance = (int)(offlineDistance);

            if (dotDistance < -320) dotDistance = -320;
            if (dotDistance > 320) dotDistance = 320;

            if (dotDistance < -10) dotDistance -= 30;
            if (dotDistance > 10) dotDistance += 30;

            // dot background
            gl.PointSize(8.0f);
            gl.Color(0.00f, 0.0f, 0.0f);
            gl.Begin(OpenGL.GL_POINTS);
            for (int i = -10; i < 0; i++) gl.Vertex((i * 40), down);
            for (int i = 1; i < 11; i++) gl.Vertex((i * 40), down);
            gl.End();

            gl.PointSize(4.0f);

            //red left side
            gl.Color(0.9750f, 0.0f, 0.0f);
            gl.Begin(OpenGL.GL_POINTS);
            for (int i = -10; i < 0; i++) gl.Vertex((i * 40), down);

            //green right side
            gl.Color(0.0f, 0.9750f, 0.0f);
            for (int i = 1; i < 11; i++) gl.Vertex((i * 40), down);
            gl.End();

                //Are you on the right side of line? So its green.
                if ((offlineDistance) < 0.0)
                {
                    int dots = dotDistance * -1 / 32;

                    gl.PointSize(32.0f);
                    gl.Color(0.0f, 0.0f, 0.0f);
                    gl.Begin(OpenGL.GL_POINTS);
                    for (int i = 1; i < dots + 1; i++) gl.Vertex((i * 40), down);
                    gl.End();

                    gl.PointSize(24.0f);
                    gl.Color(0.0f, 0.980f, 0.0f);
                    gl.Begin(OpenGL.GL_POINTS);
                    for (int i = 0; i < dots; i++) gl.Vertex((i * 40 + 40), down);
                    gl.End();
                    //return;
                }

                else
                {
                    int dots = dotDistance / 32;

                    gl.PointSize(32.0f);
                    gl.Color(0.0f, 0.0f, 0.0f);
                    gl.Begin(OpenGL.GL_POINTS);
                    for (int i = 1; i < dots + 1; i++) gl.Vertex((i * -40), down);
                    gl.End();

                    gl.PointSize(24.0f);
                    gl.Color(0.980f, 0.30f, 0.0f);
                    gl.Begin(OpenGL.GL_POINTS);
                    for (int i = 0; i < dots; i++) gl.Vertex((i * -40 - 40), down);
                    gl.End();
                    //return;
                }
            
            //yellow center dot
            if (dotDistance >= -10 && dotDistance <= 10)
            {
                gl.PointSize(32.0f);                
                gl.Color(0.0f, 0.0f, 0.0f);
                gl.Begin(OpenGL.GL_POINTS);
                gl.Vertex(0, down);
                //gl.Vertex(0, down + 50);
                gl.End();

                gl.PointSize(24.0f);
                gl.Color(0.980f, 0.98f, 0.0f);
                gl.Begin(OpenGL.GL_POINTS);
                gl.Vertex(0, down);
                //gl.Vertex(0, down + 50);
                gl.End();
            }

            else
            {

                gl.PointSize(8.0f);
                gl.Color(0.00f, 0.0f, 0.0f);
                gl.Begin(OpenGL.GL_POINTS);
                gl.Vertex(-0, down);
                //gl.Vertex(0, down + 30);
                //gl.Vertex(0, down + 60);
                gl.End();

                //gl.PointSize(4.0f);
                //gl.Color(0.9250f, 0.9250f, 0.250f);
                //gl.Begin(OpenGL.GL_POINTS);
                //gl.Vertex(0, down);
                //gl.Vertex(0, down + 30);
                //gl.Vertex(0, down + 60);
                //gl.End();
            }
        }
        
    }//endo of class
}
