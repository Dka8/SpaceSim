﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using SpaceSim.Drawing;
using SpaceSim.Engines;
using SpaceSim.Spacecrafts.FalconCommon;
using VectorMath;

namespace SpaceSim.Spacecrafts.Falcon9
{
    sealed class F9S1 : SpaceCraftBase
    {
        public override string CraftName { get { return "F9 S1"; } }
        public override string CommandFileName { get { return "F9S1.xml"; } }

        public override double DryMass { get { return 25600; } }

        public override double Width { get { return 4.11; } }
        public override double Height { get { return 47.812188; } }

        public override bool ExposedToAirFlow { get { return Parent == null; } }

        public override double DragCoefficient
        {
            get { return 1.9; }
        }

        public override double CrossSectionalArea { get { return 1.15 * Math.PI * 1.83 * 1.83; } }


        public override Color IconColor { get { return Color.White; } }

        private GridFin[] _gridFins;
        private LandingLeg[] _landingLegs;

        private Bitmap _drawingBuffer;
        private Bitmap _sootTexture;

        private double _sootRatio;

        public F9S1(string craftDirectory, DVector2 position, DVector2 velocity)
            : base(craftDirectory, position, velocity, 398887, "Textures/f9S1.png")
        {
            StageOffset = new DVector2(0, 25.5);

            Engines = new IEngine[9];

            for (int i = 0; i < 9; i++)
            {
                double engineOffsetX = (i - 4.0) / 4.0;

                var offset = new DVector2(engineOffsetX * Width * 0.3, Height * 0.45);

                Engines[i] = new Merlin1D(i, this, offset);
            }

            _gridFins = new []
            {
                new GridFin(this, new DVector2(1.3, -16.3), true),
                new GridFin(this, new DVector2(-1.3, -16.3), false)
            };

            _landingLegs = new[]
            {
                new LandingLeg(this, new DVector2(0.94, 21), true),
                new LandingLeg(this, new DVector2(-0.94, 21), false)
            };

            // Initialized 'soot' texture and allocate the drawing buffer
            _sootTexture = new Bitmap("Textures/f9S1Soot.png");
            _drawingBuffer = new Bitmap(_sootTexture.Width, _sootTexture.Height);
        }

        public override void DeployGridFins()
        {
            foreach (GridFin gridFin in _gridFins)
            {
                gridFin.Deploy();
            }
        }

        public override void DeployLandingLegs()
        {
            foreach (LandingLeg landingLeg in _landingLegs)
            {
                landingLeg.Deploy();
            }
        }

        public override void Update(double dt)
        {
            base.Update(dt);

            foreach (GridFin gridFin in _gridFins)
            {
                gridFin.Update(dt);
            }

            foreach (LandingLeg landingLeg in _landingLegs)
            {
                landingLeg.Update(dt);
            }

            DVector2 velocity = GetRelativeVelocity();
            double altitude = GetRelativeAltitude();
           
            velocity.Normalize();

            DVector2 rotation = new DVector2(Math.Cos(Rotation), Math.Sin(Rotation));

            // If we are going retro-grade and firing rockets adds soot
            if (altitude < 70000 && velocity.Dot(rotation) < 0)
            {
                foreach (IEngine engine in Engines)
                {
                    if (engine.IsActive && engine.Throttle > 0)
                    {
                        _sootRatio = Math.Min(_sootRatio + 0.01 * dt, 1.0);
                    }
                }   
            }
        }

        protected override void RenderShip(Graphics graphics, RectangleD cameraBounds, RectangleF screenBounds)
        {
            // Build the main texture (a combination of base and soot)
            using (Graphics graphics2 = RenderUtils.GetContext(false, _drawingBuffer))
            {
                if (_sootRatio > 0.99)
                {
                    graphics2.DrawImage(_sootTexture, new Rectangle(0, 0, _drawingBuffer.Width, _drawingBuffer.Height));
                }
                else if (_sootRatio < 0.05)
                {
                    graphics2.DrawImage(Texture, new Rectangle(0, 0, _drawingBuffer.Width, _drawingBuffer.Height));
                }
                else
                {
                    graphics2.DrawImage(Texture, new Rectangle(0, 0, _drawingBuffer.Width, _drawingBuffer.Height));

                    float[][] matrixAlpha =
                    {
                        new float[] {1, 0, 0, 0, 0},
                        new float[] {0, 1, 0, 0, 0},
                        new float[] {0, 0, 1, 0, 0},
                        new float[] {0, 0, 0, (float)_sootRatio, 0}, 
                        new float[] {0, 0, 0, 0, 1}
                    };

                    ColorMatrix colorMatrix = new ColorMatrix(matrixAlpha);

                    ImageAttributes iaAlphaBlend = new ImageAttributes();
                    iaAlphaBlend.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    graphics2.DrawImage(_sootTexture, new Rectangle(0, 0, _drawingBuffer.Width, _drawingBuffer.Height), 0, 0,
                                        _drawingBuffer.Width, _drawingBuffer.Height, GraphicsUnit.Pixel, iaAlphaBlend);   
                }
            }

            double drawingRotation = Rotation + Math.PI * 0.5;

            var offset = new PointF(screenBounds.X + screenBounds.Width * 0.5f,
                                    screenBounds.Y + screenBounds.Height * 0.5f);

            graphics.TranslateTransform(offset.X, offset.Y);

            graphics.RotateTransform((float)(drawingRotation * 180 / Math.PI));

            graphics.TranslateTransform(-offset.X, -offset.Y);

            graphics.DrawImage(_drawingBuffer, screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height);

            graphics.ResetTransform();

            foreach (GridFin gridFin in _gridFins)
            {
                gridFin.RenderGdi(graphics, cameraBounds);
            }

            foreach (LandingLeg landingLeg in _landingLegs)
            {
                landingLeg.RenderGdi(graphics, cameraBounds);
            }
        }
    }
}
