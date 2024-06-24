﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OneSourceSMTPServer.Data;

#nullable disable

namespace OneSourceSMTPServer.Migrations
{
    [DbContext(typeof(OneSourceContext))]
    partial class OneSourceContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.18")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("OneSourceSMTPServer.Data.Entities.MappingSMTPReceiver", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Category")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("category");

                    b.Property<string>("ContainsString")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("containsString");

                    b.Property<string>("DataAccess")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("dataAccess");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("description");

                    b.Property<string>("DestinationInstance")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("destinationInstance");

                    b.Property<string>("DestinationInstanceVersion")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("destinationInstanceVersion");

                    b.Property<bool?>("DiscardInternal")
                        .HasColumnType("bit")
                        .HasColumnName("discardInternal");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("bit")
                        .HasColumnName("isEnabled");

                    b.Property<DateTime?>("LastUpdate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("lastUpdate")
                        .HasDefaultValueSql("getdate()");

                    b.Property<string>("MenuEntryName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Mode")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("mode");

                    b.Property<string>("Section")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("section");

                    b.Property<string>("ToEmail")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("toEmail");

                    b.HasKey("Id");

                    b.ToTable("sys_MappingSMTPReceiver");
                });

            modelBuilder.Entity("OneSourceSMTPServer.Data.Entities.SMTPLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("EmlPath")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("emlPath");

                    b.Property<string>("From")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("from");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("bit")
                        .HasColumnName("isEnabled");

                    b.Property<DateTime?>("LastUpdate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasColumnName("lastUpdate")
                        .HasDefaultValueSql("getdate()");

                    b.Property<string>("Mode")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("mode");

                    b.Property<string>("RuleName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("ruleName");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("subject");

                    b.Property<string>("To")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("to");

                    b.HasKey("Id");

                    b.ToTable("sys_SMTPLog");
                });
#pragma warning restore 612, 618
        }
    }
}
