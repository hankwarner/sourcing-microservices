﻿// <auto-generated />
using System;
using ServiceSourcing.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace ServiceSourcing.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("ServiceSourcing.Models.AccountDetail", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<DateTime>("Created")
                        .HasColumnName("created");

                    b.Property<string>("Email")
                        .HasColumnName("email");

                    b.Property<DateTime>("LastModified")
                        .HasColumnName("last_modified");

                    b.HasKey("Id");

                    b.ToTable("AccountDetails");
                });

            modelBuilder.Entity("ServiceSourcing.Models.Address", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<int?>("AccountDetailId");

                    b.Property<string>("AddressLine")
                        .HasColumnName("address_line");

                    b.Property<string>("AddressType")
                        .HasColumnName("address_type");

                    b.Property<string>("City")
                        .HasColumnName("city");

                    b.Property<string>("CompanyName")
                        .HasColumnName("company_name");

                    b.Property<DateTime>("Created")
                        .HasColumnName("created");

                    b.Property<string>("FirstName")
                        .HasColumnName("first_name");

                    b.Property<DateTime>("LastModified")
                        .HasColumnName("last_modified");

                    b.Property<string>("LastName")
                        .HasColumnName("last_name");

                    b.Property<string>("PhoneNumber")
                        .HasColumnName("phone_number");

                    b.Property<bool>("Primary")
                        .HasColumnName("primary");

                    b.Property<string>("State")
                        .HasColumnName("state");

                    b.Property<string>("Zip")
                        .HasColumnName("zip");

                    b.HasKey("Id");

                    b.HasIndex("AccountDetailId");

                    b.ToTable("Addresses");
                });

            modelBuilder.Entity("ServiceSourcing.Models.Address", b =>
                {
                    b.HasOne("ServiceSourcing.Models.AccountDetail")
                        .WithMany("Addresses")
                        .HasForeignKey("AccountDetailId");
                });
#pragma warning restore 612, 618
        }
    }
}