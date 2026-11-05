using System;
using AgriSyncIndia.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AgriSyncIndia.Migrations;

[DbContext(typeof(AgriSyncDbContext))]
partial class AgriSyncDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.14")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("AgriSyncIndia.Models.Actuator", b =>
            {
                b.Property<int>("ActuatorId")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ActuatorId"));

                b.Property<string>("CurrentStatus")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("nvarchar(20)");

                b.Property<string>("DeviceType")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("nvarchar(50)");

                b.Property<bool>("IsAutoMode")
                    .HasColumnType("bit");

                b.Property<DateTime>("LastTriggered")
                    .HasColumnType("datetime2");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<double>("PowerRatingKw")
                    .HasColumnType("float");

                b.Property<int>("ZoneId")
                    .HasColumnType("int");

                b.HasKey("ActuatorId");

                b.HasIndex("ZoneId");

                b.ToTable("Actuators");
            });

        modelBuilder.Entity("AgriSyncIndia.Models.AutomationRule", b =>
            {
                b.Property<int>("RuleId")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("RuleId"));

                b.Property<string>("ActionTarget")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<string>("ConditionVariable")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("nvarchar(50)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Description")
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<bool>("IsActive")
                    .HasColumnType("bit");

                b.Property<string>("Operator")
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasColumnType("nvarchar(10)");

                b.Property<double>("ThresholdValue")
                    .HasColumnType("float");

                b.HasKey("RuleId");

                b.ToTable("AutomationRules");
            });

        modelBuilder.Entity("AgriSyncIndia.Models.ClimateAlert", b =>
            {
                b.Property<int>("AlertId")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("AlertId"));

                b.Property<string>("AgronomistRecommendation")
                    .IsRequired()
                    .HasMaxLength(250)
                    .HasColumnType("nvarchar(250)");

                b.Property<bool>("IsAcknowledged")
                    .HasColumnType("bit");

                b.Property<string>("Message")
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)");

                b.Property<string>("Severity")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("nvarchar(20)");

                b.Property<DateTime>("TimestampIST")
                    .HasColumnType("datetime2");

                b.Property<string>("Title")
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnType("nvarchar(150)");

                b.Property<int?>("ZoneId")
                    .HasColumnType("int");

                b.HasKey("AlertId");

                b.HasIndex("ZoneId");

                b.ToTable("ClimateAlerts");
            });

        modelBuilder.Entity("AgriSyncIndia.Models.SensorLog", b =>
            {
                b.Property<int>("LogId")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("LogId"));

                b.Property<double>("Humidity")
                    .HasColumnType("float");

                b.Property<double>("SoilMoisture")
                    .HasColumnType("float");

                b.Property<double>("SoilTemperature")
                    .HasColumnType("float");

                b.Property<double>("SolarRadiation")
                    .HasColumnType("float");

                b.Property<double>("Temperature")
                    .HasColumnType("float");

                b.Property<DateTime>("TimestampIST")
                    .HasColumnType("datetime2");

                b.Property<double>("VPD")
                    .HasColumnType("float");

                b.Property<double>("WindSpeed")
                    .HasColumnType("float");

                b.Property<int>("ZoneId")
                    .HasColumnType("int");

                b.HasKey("LogId");

                b.HasIndex("TimestampIST");

                b.HasIndex("ZoneId", "TimestampIST");

                b.ToTable("SensorLogs");
            });

        modelBuilder.Entity("AgriSyncIndia.Models.User", b =>
            {
                b.Property<int>("UserId")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserId"));

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Email")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<string>("FullName")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<string>("PasswordHash")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");

                b.Property<string>("Role")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("nvarchar(50)");

                b.HasKey("UserId");

                b.HasIndex("Email")
                    .IsUnique();

                b.ToTable("Users");
            });

        modelBuilder.Entity("AgriSyncIndia.Models.Zone", b =>
            {
                b.Property<int>("ZoneId")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ZoneId"));

                b.Property<double>("AreaSqMeters")
                    .HasColumnType("float");

                b.Property<string>("CropType")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<string>("Description")
                    .IsRequired()
                    .HasMaxLength(250)
                    .HasColumnType("nvarchar(250)");

                b.Property<string>("IconClass")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("nvarchar(50)");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<string>("Status")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("nvarchar(50)");

                b.Property<double>("TargetHumidity")
                    .HasColumnType("float");

                b.Property<double>("TargetTemp")
                    .HasColumnType("float");

                b.HasKey("ZoneId");

                b.ToTable("Zones");
            });

        modelBuilder.Entity("AgriSyncIndia.Models.Actuator", b =>
            {
                b.HasOne("AgriSyncIndia.Models.Zone", "Zone")
                    .WithMany("Actuators")
                    .HasForeignKey("ZoneId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Zone");
            });

        modelBuilder.Entity("AgriSyncIndia.Models.ClimateAlert", b =>
            {
                b.HasOne("AgriSyncIndia.Models.Zone", "Zone")
                    .WithMany("Alerts")
                    .HasForeignKey("ZoneId")
                    .OnDelete(DeleteBehavior.SetNull);

                b.Navigation("Zone");
            });

        modelBuilder.Entity("AgriSyncIndia.Models.SensorLog", b =>
            {
                b.HasOne("AgriSyncIndia.Models.Zone", "Zone")
                    .WithMany("SensorLogs")
                    .HasForeignKey("ZoneId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Zone");
            });

        modelBuilder.Entity("AgriSyncIndia.Models.Zone", b =>
            {
                b.Navigation("Actuators");

                b.Navigation("Alerts");

                b.Navigation("SensorLogs");
            });
#pragma warning restore 612, 618
    }
}
