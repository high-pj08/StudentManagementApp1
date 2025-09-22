using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentManagementApp.Models;

namespace StudentManagementApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; } = default!;
        public DbSet<Teacher> Teachers { get; set; } = default!;
        public DbSet<Subject> Subjects { get; set; } = default!;
        public DbSet<Class> Classes { get; set; } = default!;
        public DbSet<Enrollment> Enrollments { get; set; } = default!;
        public DbSet<TeacherClassSubject> TeacherClassSubjects { get; set; } = default!;
        public DbSet<Attendance> Attendances { get; set; } = default!;
        public DbSet<Exam> Exams { get; set; } = default!;
        public DbSet<Mark> Marks { get; set; } = default!;
        public DbSet<Parent> Parents { get; set; } = default!;
        public DbSet<Payment> Payments { get; set; } = default!;

        // Existing DbSets for Fee Management
        public DbSet<FeeType> FeeTypes { get; set; } = default!;
        public DbSet<ClassFee> ClassFees { get; set; } = default!;
        public DbSet<StudentFee> StudentFees { get; set; } = default!;

        // New DbSets for Invoicing
        public DbSet<Invoice> Invoices { get; set; } = default!;
        public DbSet<InvoiceItem> InvoiceItems { get; set; } = default!;
        public DbSet<Notice> Notices { get; set; } = default!;
        public DbSet<Holiday> Holidays { get; set; } = default!;


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // IMPORTANT: Call base.OnModelCreating for Identity tables

            // Configure the many-to-many relationship between Student and Parent
            // This setup creates a join table and by default doesn't have cascade delete on the join table records
            builder.Entity<Student>()
                .HasMany(s => s.Parents)
                .WithMany(p => p.Children)
                .UsingEntity(j => j.ToTable("StudentParent"));

            // Configure unique constraint for ClassFee to prevent duplicate fee types per class
            builder.Entity<ClassFee>()
                .HasIndex(cf => new { cf.ClassId, cf.FeeTypeId })
                .IsUnique();

            // Configure unique constraint for StudentFee to prevent duplicate fee types per student
            builder.Entity<StudentFee>()
                .HasIndex(sf => new { sf.StudentId, sf.FeeTypeId })
                .IsUnique();

            // --- COMPREHENSIVE CASCADE FIX: Set DeleteBehavior.Restrict for all FKs targeting Student or Parent ---

            // Payment to Student
            builder.Entity<Payment>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict); // Set to RESTRICT

            // Payment to Parent
            builder.Entity<Payment>()
                .HasOne(p => p.Parent)
                .WithMany()
                .HasForeignKey(p => p.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // Set to RESTRICT

            // Invoice to Student
            builder.Entity<Invoice>()
                .HasOne(i => i.Student)
                .WithMany()
                .HasForeignKey(i => i.StudentId)
                .OnDelete(DeleteBehavior.Restrict); // Set to RESTRICT

            // Invoice to Parent
            builder.Entity<Invoice>()
                .HasOne(i => i.Parent)
                .WithMany()
                .HasForeignKey(i => i.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // Set to RESTRICT

            // Enrollment to Student
            builder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict); // Set to RESTRICT

            // Enrollment to Class (Student to Class relationship)
            builder.Entity<Enrollment>()
                .HasOne(e => e.Class)
                .WithMany(c => c.Enrollments) // Assuming Class has Enrollments ICollection
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Restrict); // Set to RESTRICT (or Cascade if deleting Class should delete enrollments)

            // Attendance to Student
            builder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict); // Set to RESTRICT

            // Mark to Student
            builder.Entity<Mark>()
                .HasOne(m => m.Student)
                .WithMany()
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.Restrict); // Set to RESTRICT

            // StudentFee to Student
            builder.Entity<StudentFee>()
                .HasOne(sf => sf.Student)
                .WithMany()
                .HasForeignKey(sf => sf.StudentId)
                .OnDelete(DeleteBehavior.Restrict); // Set to RESTRICT

            // --- Other relationships (keep existing or set to desired behavior) ---

            // Invoice to InvoiceItems: If invoice is deleted, its items are deleted (this is usually desired)
            builder.Entity<Invoice>()
                .HasMany(i => i.InvoiceItems)
                .WithOne(ii => ii.Invoice)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade); // KEEP Cascade for child items

            // Invoice to Payments: If invoice is deleted, set Payment.InvoiceId to null (don't delete payment)
            // This should now be fine because deletions of Student/Parent are restricted.
            builder.Entity<Invoice>()
                .HasMany(i => i.Payments)
                .WithOne(p => p.Invoice)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.SetNull); // KEEP SetNull

            // InvoiceItem to FeeType: Prevent deleting FeeType if it's used in an InvoiceItem
            builder.Entity<InvoiceItem>()
                .HasOne(ii => ii.FeeType)
                .WithMany()
                .HasForeignKey(ii => ii.FeeTypeId)
                .OnDelete(DeleteBehavior.Restrict); // KEEP Restrict

            // ClassFee to Class
            builder.Entity<ClassFee>()
                .HasOne(cf => cf.Class)
                .WithMany()
                .HasForeignKey(cf => cf.ClassId)
                .OnDelete(DeleteBehavior.Cascade); // If deleting a Class should delete its ClassFees

            // ClassFee to FeeType
            builder.Entity<ClassFee>()
                .HasOne(cf => cf.FeeType)
                .WithMany()
                .HasForeignKey(cf => cf.FeeTypeId)
                .OnDelete(DeleteBehavior.Restrict); // Changed to RESTRICT - don't delete FeeType if it's used in ClassFee

            // StudentFee to FeeType
            builder.Entity<StudentFee>()
                .HasOne(sf => sf.FeeType)
                .WithMany()
                .HasForeignKey(sf => sf.FeeTypeId)
                .OnDelete(DeleteBehavior.Restrict); // Changed to RESTRICT - don't delete FeeType if it's used in StudentFee

            // Marks to Exam, Subject, Class, Teacher (if applicable) - review these similarly
            builder.Entity<Mark>()
                .HasOne(m => m.Exam)
                .WithMany()
                .HasForeignKey(m => m.ExamId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict deleting Exam if marks exist

            builder.Entity<Mark>()
                .HasOne(m => m.Subject)
                .WithMany()
                .HasForeignKey(m => m.SubjectId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict deleting Subject if marks exist

            builder.Entity<Mark>()
                .HasOne(m => m.Class)
                .WithMany()
                .HasForeignKey(m => m.ClassId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict deleting Class if marks exist

            // TeacherClassSubject relationships
            builder.Entity<TeacherClassSubject>()
                .HasOne(tcs => tcs.Teacher)
                .WithMany(t => t.TeacherClassSubjects)
                .HasForeignKey(tcs => tcs.TeacherId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict deleting teacher if assignments exist

            builder.Entity<TeacherClassSubject>()
                .HasOne(tcs => tcs.Class)
                .WithMany(c => c.TeacherClassSubjects)
                .HasForeignKey(tcs => tcs.ClassId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict deleting class if assignments exist

            builder.Entity<TeacherClassSubject>()
                .HasOne(tcs => tcs.Subject)
                .WithMany(s => s.TeacherClassSubjects)
                .HasForeignKey(tcs => tcs.SubjectId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict deleting subject if assignments exist

            // Attendance relationships
            builder.Entity<Attendance>()
                .HasOne(a => a.Class)
                .WithMany()
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict deleting class if attendance exists
        }
    }
}
