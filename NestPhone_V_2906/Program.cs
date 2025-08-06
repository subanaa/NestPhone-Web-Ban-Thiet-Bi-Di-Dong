using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NestPhone.Repositoires.ChiTietKhuyenMaiSQL;
using NestPhone.Repositoires.ChiTietSanPhamSQL;
using NestPhone.Repositoires.DanhGiaSQL;
using NestPhone.Repositoires.DonHangSQL;
using NestPhone.Repositoires.DungLuongSQL;
using NestPhone.Repositoires.HinhAnhSQL;
using NestPhone.Repositoires.KhachHangSQL;
using NestPhone.Repositoires.KhoSQL;
using NestPhone.Repositoires.KhuyenMaiSQL;
using NestPhone.Repositoires.MauSacSQL;
using NestPhone.Repositoires.NhanVienSQL;
using NestPhone.Repositoires.PhieuNhapKhoSQL;
using NestPhone.Repositoires.SanPhamSQL;
using NestPhone.Repositoires.ThongSoKiThuatSQL;
using NestPhone.Repositoires.ThuongHieuSQL;
using NestPhone.Repositories.DuocDatSQL;
using NestPhone.Repositories.KhuyenMaiSQL;
using NestPhone.Repositories.MauSacSQL;
using NestPhone_V_2406.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DbContext with SQL Server
builder.Services.AddDbContext<NestPhoneAPI>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NestPhoneConnectionStrings")));

// Register repositorieskh
builder.Services.AddScoped<IKhacHangRepo, KhachHangRepository>();
builder.Services.AddScoped<IDonHangRepo,DonHangRepository>();
builder.Services.AddScoped<IMauSacRepo, MauSacRepository>();
builder.Services.AddScoped<IDungLuongRepo,DungLuongRepository>();
builder.Services.AddScoped<IThuongHieuRepo,ThuongHieuRepository>();
builder.Services.AddScoped<IKhuyenMaiRepo,KhuyenMaiRepository>();
builder.Services.AddScoped<INhanVienRepo,NhanVienRepository>();
builder.Services.AddScoped<IKhoRepo,KhoRepository>();
builder.Services.AddScoped<ISanPhamRepo,SanPhamRepository>();
builder.Services.AddScoped<IPhieuNhapKhoRepo,PhieuNhapKhoRepository>();
builder.Services.AddScoped<IDanhGiaRepo,DanhGiaRepository>();
builder.Services.AddScoped<IChiTietSanPhamRepo,ChiTietSanPhamRepository>();
builder.Services.AddScoped<IHinhAnhRepo,HinhAnhRepository>();
builder.Services.AddScoped<IThongSoKiThuatRepo, ThongSoKiThuatRepository>();
builder.Services.AddScoped<IChiTietKhuyenMaiRepo, ChiTietKhuyenMaiRepository>();
builder.Services.AddScoped<IDuocDatRepo,DuocDatRepository>();

/// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();

app.UseCors("AllowAllOrigins");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();