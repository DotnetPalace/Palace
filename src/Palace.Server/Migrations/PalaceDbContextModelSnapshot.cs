﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Palace.Server.Services;

#nullable disable

namespace Palace.Server.Migrations
{
    [DbContext(typeof(PalaceDbContext))]
    partial class PalaceDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.9");

            modelBuilder.Entity("Palace.Shared.MicroServiceSettings", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<bool>("AlwaysStarted")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Arguments")
                        .HasColumnType("TEXT");

                    b.Property<string>("GroupName")
                        .HasColumnType("TEXT");

                    b.Property<string>("MainAssembly")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long?>("MaxWorkingSetLimitBeforeAlert")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("MaxWorkingSetLimitBeforeRestart")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("NoHealthCheckCountBeforeRestart")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("NoHealthCheckCountCountBeforeAlert")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PackageFileName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ServiceName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("ThreadLimitBeforeAlert")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ThreadLimitBeforeRestart")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("MicroServiceSetting", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
