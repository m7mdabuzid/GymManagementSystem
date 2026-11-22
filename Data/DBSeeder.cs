using GymManagementSystem.Models;

namespace GymManagementSystem.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext db)
        {
            if (db.Trainers.Any()) return;

            var t1 = new Trainer { Name = "Ali", Specialization = "Bodybuilding" };
            var t2 = new Trainer { Name = "Sara", Specialization = "Yoga" };

            db.Trainers.AddRange(t1, t2);
            db.SaveChanges();

            db.ClassSessions.AddRange(
                new ClassSession
                {
                    ClassName = "Morning HIIT",
                    TrainerId = t1.TrainerId,
                    ScheduleTime = DateTime.Today.AddHours(9),
                    MaxSlots = 10
                },
                new ClassSession
                {
                    ClassName = "Evening Yoga",
                    TrainerId = t2.TrainerId,
                    ScheduleTime = DateTime.Today.AddHours(18),
                    MaxSlots = 8
                }
            );

            db.Members.Add(new Member
            {
                Name = "Mohammad",
                MembershipType = MembershipType.Monthly,
                IsActive = true,
                MembershipEndDate = DateTime.Today.AddMonths(1)
            });

            db.SaveChanges();
        }
    }
}
