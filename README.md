# Better Unreleased (Early Access) Depracted (v2 Soon)

a music player to play local files aka unreleased songs

### Download and Building


### Download
You either can Download this Project from the Latest Release or 
from [S42.site](https://S42.site/shop).



### Build
If you want to Build it yourself first copy or fork the Repo.
Also you need these things:

[DOTNET Framework Version 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
[Entity Framework Core Tools](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Tools)
```sh
dotnet tool install --global dotnet-ef
```


When you have everything its time to build. First use this command:

```sh
dotnet restore
```

After that use this command to restore create the Database file (but this isnt mendatory because on start of the application it makes a Database):

```sh
dotnet ef database update
```


Now to build the app itself you need to use this command:
```sh
dotnet build
```
 and then start the Application with 

 ```sh
 dotnet start
 ```


 ### Database Migration

If you want to make a new model or change a Database model after you added you code you need to use these commands:

```sh
dotnet ef migrations add {Migration Name}
```

{Migration Name} can be any if it doesnt exist yet.

After that use 

```sh
dotnet ef database update
```

again.

## Features

It has all Play Features of a Standart Music Player and also Playlist Functions

### UI
The UI is a Alpha UI and will be changed to look better



## Details

### License:
This project is Licensed under a MIT [License](LICENSE) <--- Click here for more Details.

### Contribution
You can open a Issue with a Feature Request or a Pull Request with the Feature you want to see! Im Happy to help you.
