using System;

namespace SeedProject.Persistence.Model
{
    public class AbsenceRequest
    {
        public int Id { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsHalfDayStart { get; set; }

        public bool IsHalfDayEnd { get; set; }

        public string Description { get; set; }
    }
}