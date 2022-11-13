using ElectronNET.API;
using ElectronNET.API.Entities;
using MediatR;
using U2pa.Electron.Link.Handlers.Rom;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddElectron();
builder.WebHost.UseElectron(args);
builder.Services.AddMediatR(typeof(ReadCommand));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error");
}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

BootStrap();

app.Run();

async void BootStrap()
{
  var options = new BrowserWindowOptions
  {
    Show = false,
  };

  var mainWindow = await Task.Run(async () => await ElectronNET.API.Electron.WindowManager.CreateWindowAsync(options));
  mainWindow.OnReadyToShow += () => mainWindow.Show();

  ElectronNET.API.Electron.Menu.SetApplicationMenu(new MenuItem[0]);
}
