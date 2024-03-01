﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SMTPServer.Data;

#nullable disable

namespace SMTPServer.Migrations
{
    [DbContext(typeof(OneSourceContext))]
    [Migration("20240227115245_AddedSMTPLogTable")]
    partial class AddedSMTPLogTable
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.16")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("SMTPServer.Data.Entities.SMTPLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

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