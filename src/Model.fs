module BigHack.Fumble.Api.Model

type Card =
    { CardTitle : string
      CardContent : string }

type CardCollection =
    {  Name : string
       Cards : Card list }