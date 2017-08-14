using System;

namespace CuponRedeemer
{
    class Cupon
    {
        //properties
        public string ContentId
        {
            get
            {
                return contentId;
            }
        }
        public string Code
        {
            get
            {
                return cuponString;
            }
        }
        public DateTime ExpireTime
        {
            get
            {
                return redeemTime;
            }
        }
        public bool Valid
        {
            get
            {
                return valid;
            }
        }
        //fields
        private string contentId;
        private DateTime redeemTime;
        private string cuponString;
        private bool valid = true;

        /// <summary>
        /// Create cupon
        /// </summary>
        /// <param name="time">Expire date</param>
        /// <param name="cupon">Cupon string</param>
        /// <param name="content">Content ID</param>
        public Cupon(DateTime expireTime, string cupon, string content, bool valid = true)
        {
            redeemTime = expireTime;
            cuponString = cupon;
            contentId = content;
            this.valid = valid;
        }

        /// <summary>
        /// Deactivate cupon
        /// </summary>
        public void Disable()
        {
            valid = false;
        }

    }
}
