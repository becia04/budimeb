using System.Collections.Generic;
using System;

namespace budimeb.Models
{
    public class Project
    {
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public Category Category { get; set; }
        public ICollection<Photo> Photos { get; set; }
    }
}
