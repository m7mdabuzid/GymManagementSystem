using System;
using System.Collections.Generic;

namespace GymManagementSystem.Models
{
    /// <summary>
    /// Monthly / Yearly membership.
    /// </summary>
    public enum MembershipType
    {
        Monthly = 1,
        Yearly = 2
    }

    /// <summary>
    /// Represents a gym member.
    /// </summary>
    public class Member
    {
        public int MemberId { get; set; }

        public string Name { get; set; } = string.Empty;

        public MembershipType MembershipType { get; set; }

        /// <summary>
        /// True if the membership is currently active (paid & not expired).
        /// You can derive this from dates if you prefer.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Optional: when the membership ends.
        /// </summary>
        public DateTime? MembershipEndDate { get; set; }

        /// <summary>
        /// Optional: fitness progress (weight, calories, notes...).
        /// One Member -> Many Progress entries
        /// </summary>
        public List<FitnessProgressEntry> ProgressLog { get; set; } = new();

        /// <summary>
        /// Many-to-many with ClassSession via Booking join table.
        /// </summary>
        public List<Booking> Bookings { get; set; } = new();

        /// <summary>
        /// Helper to check if member can book.
        /// </summary>
        public bool CanBookClass()
        {
            if (!IsActive)
                return false;

            if (MembershipEndDate.HasValue && MembershipEndDate.Value < DateTime.Now)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Represents a trainer (yoga, HIIT, bodybuilding, etc.).
    /// </summary>
    public class Trainer
    {
        public int TrainerId { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// e.g. "Yoga", "HIIT", "Bodybuilding".
        /// </summary>
        public string Specialization { get; set; } = string.Empty;

        /// <summary>
        /// One Trainer -> Many ClassSessions
        /// </summary>
        public List<ClassSession> AssignedClasses { get; set; } = new();
    }

    /// <summary>
    /// Represents a scheduled fitness class session.
    /// </summary>
    public class ClassSession
    {
        public int SessionId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// FK + navigation for Trainer (required).
        /// </summary>
        public int TrainerId { get; set; }
        public Trainer Trainer { get; set; } = null!;

        /// <summary>
        /// When the session takes place.
        /// </summary>
        public DateTime ScheduleTime { get; set; }

        /// <summary>
        /// Maximum number of members allowed.
        /// </summary>
        public int MaxSlots { get; set; }

        /// <summary>
        /// Many-to-many with Member via Booking join table.
        /// </summary>
        public List<Booking> Bookings { get; set; } = new();
    }

    /// <summary>
    /// Join table: Member books a ClassSession.
    /// </summary>
    public class Booking
    {
        public int BookingId { get; set; }

        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;

        public int SessionId { get; set; }
        public ClassSession Session { get; set; } = null!;

        public DateTime BookedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Optional class to track progress / calories / notes per member.
    /// </summary>
    public class FitnessProgressEntry
    {
        public int Id { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        /// <summary>
        /// Optional: estimated calories burned.
        /// </summary>
        public double? CaloriesBurned { get; set; }

        /// <summary>
        /// Optional: weight, reps, or any metric you want.
        /// </summary>
        public double? Weight { get; set; }

        /// <summary>
        /// Free text notes (e.g., "Improved endurance", "New PR on squats").
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// FK + navigation back to the member (required).
        /// </summary>
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;
    }
}
