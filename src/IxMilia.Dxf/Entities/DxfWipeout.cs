// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.Dxf.Entities
{
    public partial class DxfWipeout
    {
        /// <param name="imagePath">The path to the image.</param>
        /// <param name="location">The bottom left corner of the location to display the image in the drawing.</param>
        /// <param name="imageWidth">The width of the image in pixels.</param>
        /// <param name="imageHeight">The height of the image in pixels.</param>
        /// <param name="displaySize">The display size of the image in drawing units.</param>
        public DxfWipeout(string imagePath, DxfPoint location, int imageWidth, int imageHeight, DxfVector displaySize)
            : base(imagePath, location, imageWidth, imageHeight, displaySize)
        {
        }
    }
}
