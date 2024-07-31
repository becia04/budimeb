using System;

namespace budimeb.Models
{
    public class Photo
    {
        public int PhotoId { get; set; }
        public int ProjectId { get; set; }
        public string PhotoPath { get; set; }
        public string Description { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.Now;

        public Project Project { get; set; }
    }
}
