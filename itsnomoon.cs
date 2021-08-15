using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Effects;
using Jypeli.Widgets;

/// @author  Juuso Huovila
/// @version 13.11.2019

public class itsnomoon : PhysicsGame
{

    const int RUUDUN_KOKO = 40;

    // Luodaan pelaaja ja hänelle ase
    PlatformCharacter pelaaja;
    PlasmaCannon pelaajanAse;

    // Pelissä käytetyt kuvatiedostot
    Image pelaajanKuva = LoadImage("tie2");  // pelaaja
    Image vihollisenKuva = LoadImage("xwing"); // vihollinen
    Image toisenvihollisenKuva = LoadImage("toinenvihollinen1"); // toinen vihollinen
    Image pallonKuva = LoadImage("spallo");  // sininen bonuspistepallo
    Image elamaKuva = LoadImage("pupallo1");  // pupallo = punainen elämä pallo
    Image puunKuva = LoadImage("puu"); // puu

    // Pelissä käytetyt äänet
    SoundEffect kuolema = LoadSoundEffect("wilhelm"); // Ääniefekti kun vihollinen kuolee
    SoundEffect omakuolema = LoadSoundEffect("Hl2_Rebel-Ragdoll485-573931361"); // Ääniefekti kun törmää
    SoundEffect osuma = LoadSoundEffect("Pain-SoundBible.com-1883168362"); // Ääniefekti kun itse kuolee
    SoundEffect sipallo = LoadSoundEffect("sms-alert-2-daniel_simon"); // Ääniefekti kun saat lisäpisteitä tai lisäelämän

    // Lisätään peliin Laskurit
    IntMeter pisteLaskuri;
    IntMeter elamalaskuri;
    IntMeter Aaltolaskuri;
    IntMeter Vaikeustaso;
    DoubleMeter alaspainLaskuri;
    Timer aikaLaskuri;

    //Vihollisalusten liikkumisen määrittelyä listan avulla
    private List<GameObject> liikutettavat = new List<GameObject>();
    private double suunta = -5;
    private double tuhoamisX;

    // Highscorelista
    EasyHighScore topLista = new EasyHighScore();

    /// <summary>
    /// Luo satunnaisen spawnipisteen bonus- ja elämäpalloille.
    /// </summary>
    /// <returns></returns>
    Vector bonuspaikka()
    {
        // Arvotaan bonuspalloille paikat kentältä
        Vector bonuspaikka = Level.GetRandomPosition();
        return bonuspaikka;
    }


    /// <summary>
    /// Luo taulukon avulla spawnipisteet vihollisaluksille (X-wingeille ja Millenium Falconeille)
    /// </summary>
    /// <returns></returns>
    Vector UusiVektori()
    {
        // Arvotaan ennaltamääärätyistä pisteistä jokin aina seuraavaksi spawnipisteeksi
        int[] SpawniPisteet = { 200, 100,50,0,-50,-100,-150};
        int rand = RandomGen.SelectOne(SpawniPisteet);
        Vector vektori = new Vector(500, rand);
        return vektori;
    }


    /// <summary>
    /// Käynnistää kaikki tarvittavat aliohjelmat, joita peli tarvitsee pyöriäkseen ja luo päävalikon.
    /// </summary>
    public override void Begin()
    {
        // Kokonäytön tila
        // IsFullScreen = true; 

        // Käynnistetään aliohjelmat
        LisaaNappaimet();
        LuoAaltoLaskuri();
        LuoVaikeus();
        SeuraavaAalto();

        MediaPlayer.IsRepeating = true; // Musiikki ei lopu
        MediaPlayer.Play("Electronic-ambient-background-beat"); // taustamusiikki

        // Asetetaan pelin zoomi sopivaksi
        Camera.ZoomFactor = 0.5;
        Camera.StayInLevel = true;

        // Liikuttaa vihollisaluksia vasemmalta oikealle
        tuhoamisX = Level.Left;
        Timer liikutusAjastin = new Timer();
        liikutusAjastin.Interval = 0.05;
        liikutusAjastin.Timeout += LiikutaOlioita;
        liikutusAjastin.Start();

        // Päävalikko
        IsPaused = true;
        MultiSelectWindow alkuValikko = new MultiSelectWindow("IT'S NO MOON- Protector I", "Aloita peli", "Parhaat pisteet", "Lopeta");
        Add(alkuValikko);
        alkuValikko.AddItemHandler(0, ValitseHaastavuus);
        alkuValikko.AddItemHandler(1, Highscore);
        alkuValikko.AddItemHandler(2, Exit);

    }


    /// <summary>
    /// Luo kentät aallosta riippuen ja asettaa vihollisten syntynopeuden 
    /// vaikeustasosta riippuen. Lisää myös bonus. ja elämäpallot.
    /// </summary>
    public void LuoKentta()
    {
        // Kenttä vaihtuu neljännen aallon kohdalla. Siihen asti käytössä on kenttä 1
        if (Aaltolaskuri == 1)
        {
            TileMap kentta = TileMap.FromLevelAsset("itsnomoon 2");
            kentta.SetTileMethod('#', LisaaTaso);
            kentta.SetTileMethod('*', LisaaVihollinen);
            kentta.SetTileMethod('N', LisaaPelaaja);
            kentta.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
            Level.CreateBorders();
            Level.Background.CreateStars();
            Level.Background.CreateStars(1000);
        }
        else
        {
            TileMap kentta2 = TileMap.FromLevelAsset("itsnomoon 4");
            kentta2.SetTileMethod('#', LisaaTaso);
            kentta2.SetTileMethod('*', LisaaPuu);
            kentta2.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
            Level.CreateBorders();
            Level.Background.CreateGradient(Color.Black, Color.Blue);
        }

        // Luodaan kovimmille pelaajille uusi vihollinen neljännestä aallosta eteenpäin
        if (Vaikeustaso == 3 && Aaltolaskuri > 3)
        {
            Timer ajastin2 = new Timer();
            ajastin2.Interval = 4;
            ajastin2.Timeout += delegate { LisaaToinenVihollinen(UusiVektori(), 150, 150); };
            ajastin2.Start();
        }

        // Vihollisten syntynopeus
        Timer ajastin = new Timer();
        if (Aaltolaskuri == 1)
        {
            if (Vaikeustaso == 1) { ajastin.Interval = 1; }
            else if (Vaikeustaso == 2) { ajastin.Interval = 0.8; }
            else if (Vaikeustaso == 3) { ajastin.Interval = 0.6; }
            ajastin.Timeout += delegate { LisaaVihollinen(UusiVektori(),50, 50); };
            ajastin.Start();
        }
        else 
        {
            if (Vaikeustaso == 1) { ajastin.Interval = 0.7; }
            else if (Vaikeustaso == 2) { ajastin.Interval = 0.5; }
            else if (Vaikeustaso == 3) { ajastin.Interval = 0.3; }
            ajastin.Timeout += delegate { LisaaVihollinen(UusiVektori(), 50, 50); };
            ajastin.Start();
        }

        // Bonuspallojen syntynopeus
        Timer kello1 = new Timer();
        kello1.Interval = 5;
        kello1.Timeout += delegate { LisaaBonus(bonuspaikka(), 40, 40); };
        kello1.Start();

        // Elämäpallojen syntynopeus
        Timer kello2 = new Timer();
        kello2.Interval = 15;
        kello2.Timeout += delegate { LisaaElamaPallo(bonuspaikka(), 30, 30); };
        kello2.Start();
    }


    /// <summary>
    /// Asettaa aallosta ja vaikeustasosta riippuen vihollisille syntynopeuden
    /// </summary>
    public void Aallot()
    {
        //if-lause kasvattaa vihollisten syntynopeutta aallosta ja vaikeustasosta riippuen.
        // Ensimmäiseksi katsotaan millä aallolla pelaaja on ja sitten mikä vaikeustaso.

        Timer ajastin = new Timer();
        if (Aaltolaskuri == 2)
        {
            if (Vaikeustaso == 1) { ajastin.Interval = 0.9; }
            else if (Vaikeustaso == 2) { ajastin.Interval = 0.7; }
            else if (Vaikeustaso == 3) { ajastin.Interval = 0.6; }
        }
        else if (Aaltolaskuri == 3)
        {
            if (Vaikeustaso == 1) { ajastin.Interval = 0.8; }
            else if (Vaikeustaso == 2) { ajastin.Interval = 0.6; }
            else if (Vaikeustaso == 3) { ajastin.Interval = 0.5; }
        }
        else if (Aaltolaskuri == 5)
        {
            if (Vaikeustaso == 1) { ajastin.Interval = 0.6; }
            else if (Vaikeustaso == 2) { ajastin.Interval = 0.4; }
            else if (Vaikeustaso == 3) { ajastin.Interval = 0.2; }
        }
        else if (Aaltolaskuri == 6)
        {
            if (Vaikeustaso == 1) { ajastin.Interval = 0.5; }
            else if (Vaikeustaso == 2) { ajastin.Interval = 0.3; }
            else if (Vaikeustaso == 3) { ajastin.Interval = 0.1; }
        }
        ajastin.Timeout += delegate { LisaaVihollinen(UusiVektori(), 50, 50); };
        ajastin.Start();
    }


    /// <summary>
    /// Aliohjelma, joka vastaa, siitä milloin kenttä vaihtuu ja peli loppuu.
    /// </summary>
    public void SeuraavaAalto()
    {
        // Ikkunan syntyessä peli menee pauselle, niin tässä se vapautetaan ja kutsutaan 
        // if-lauseella seuraavaa aaltoa. Neljännen aallon kohdalla kenttä vaihdetaan. Seitsemännen aallon kohdalla peli loppuu.
        IsPaused = false;
        if (Aaltolaskuri == 1 || Aaltolaskuri == 4) LuoKentta();
        else if (Aaltolaskuri <= 6) Aallot();
        else if (Aaltolaskuri == 7) PelaajaHavisi();
    }


    /// <summary>
    /// Lisää peliin tason ominaisuudet kentän mukaan
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    public void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        // Ensimmäisen kentän ajan luodaan hopeinen death star ja toiselle kentälle kiva planeetta
        if (Aaltolaskuri <= 3)
        {
            PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
            taso.Position = paikka;
            taso.Color = Color.SlateGray;
            Add(taso);
        }
        else
        {
            PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
            taso.Position = paikka;
            taso.Color = Color.Green;
            Add(taso);
        }
    }


    /// <summary>
    /// Lisää peliin puun toiseen kenttään
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    public void LisaaPuu(Vector paikka, double leveys, double korkeus)
    {
        //Lisätään puu, joka tuhoutuu ampumalla
        PhysicsObject puu = PhysicsObject.CreateStaticObject(50, 100);
        puu.IgnoresCollisionResponse = true;
        puu.Position = paikka;
        puu.Image = puunKuva;
        puu.Tag = "puu";
        Add(puu);
    }


    /// <summary>
    /// Aliohjelma lisää pelaajan ja asettaa hänelle ominaisuudet
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    public void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        // Lisätään pelaaja, hänen ominaisuutensa ja suhteensa bonus-ja elämäpalloon, sekä vihollisiin. Annetaan myös ase.
        pelaaja = new PlatformCharacter(leveys, korkeus);
        pelaaja.Position = paikka;
        pelaaja.Mass = 4.0;
        pelaaja.Image = pelaajanKuva;
        AddCollisionHandler(pelaaja, "xwing", TormaaViholliseen);
        AddCollisionHandler(pelaaja, "toinenvihollinen1", TormaaViholliseen);
        AddCollisionHandler(pelaaja, "spallo", TormaaBonukseen);
        AddCollisionHandler(pelaaja, "pupallo1", TormaaElamaan);
        Add(pelaaja);

        //Ase
        pelaajanAse = new PlasmaCannon(0, 0);
        pelaajanAse.ProjectileCollision = AmmusOsui;
        pelaaja.Add(pelaajanAse);
    } 


    /// <summary>
    /// Asettaa pelaajalle näppäimet, joilla pelata.
    /// </summary>
    public void LisaaNappaimet()
    {
        // Luodaan liikkumiseen tarvittavat näppäimet.

        //Ohjeet ja pelin lopetus
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        //Liikkuminen
        Keyboard.Listen(Key.Left, ButtonState.Down, LiikutaPelaajaa, null, new Vector(-100000, 0));
        Keyboard.Listen(Key.Right, ButtonState.Down, LiikutaPelaajaa, null, new Vector(100000, 0));
        Keyboard.Listen(Key.Up, ButtonState.Down, LiikutaPelaajaa, null, new Vector(0, 10000));
        Keyboard.Listen(Key.Down, ButtonState.Down, LiikutaPelaajaa, null, new Vector(0, -10000));

        //Ampuminen
        Keyboard.Listen(Key.Space, ButtonState.Pressed, AmmuAseella, "Ammu", pelaajanAse);
        #if DEBUG
            Keyboard.Listen(Key.S, ButtonState.Down, AmmuAseella, "Ammu", pelaajanAse); // Godmode
        #endif
    }


    /// <summary>
    /// Asettaa pelaajalle vektorin, joka määrää miten pelaaja liikkuu
    /// </summary>
    /// <param name="vektori"></param>
    public void LiikutaPelaajaa(Vector vektori)
    {
        pelaaja.Push(vektori);
    }


    /// <summary>
    /// Aliohjelma liikuttaa vihollisia vasemmalle.
    /// </summary>
    private void LiikutaOlioita()
    {
        // liikutetaan vihollisia näytöllä oikealta vasemmalle ja kun vihollinen on näytön vasemmassa laidassa se tuhoutuu
        for (int i = 0; i < liikutettavat.Count; i++)
        {
            GameObject olio = liikutettavat[i];
            olio.X += suunta;
            if (olio.X <= tuhoamisX)
            {
                olio.Destroy();
                liikutettavat.Remove(olio);
            }
        }
    }


    /// <summary>
    /// Lisää kenttään X-wing fighterin aina kutsuttaessa.
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    public void LisaaVihollinen(Vector paikka, double leveys, double korkeus)
    {
        //  Lisätään vihollisia pelin alussa halutuille paikoille
        // ja määrätään miltä ne näyttävät
        PhysicsObject Xwing = new PhysicsObject(leveys, korkeus);
        Xwing.IgnoresCollisionResponse = true;
        Xwing.Position = paikka;
        Xwing.Image = vihollisenKuva;
        Xwing.Tag = "xwing";
        Add(Xwing);
        liikutettavat.Add(Xwing);
    }


    /// <summary>
    /// Lisää peliin vaikeimmalla vaikeusasteella kolmannen aallon jälkeen Millenium Falconin kutsuttaessa.
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    public void LisaaToinenVihollinen(Vector paikka, double leveys, double korkeus)
    {
        //  Lisätään yllätysvihollinen vaikeimmalle vaikeusasteelle neljännestä aallosta eteenpäin

        PhysicsObject ToinenVihollinen = new PhysicsObject(leveys, korkeus);
        ToinenVihollinen.IgnoresCollisionResponse = true;
        ToinenVihollinen.Position = paikka;
        ToinenVihollinen.Image = toisenvihollisenKuva;
        ToinenVihollinen.Tag = "toinenvihollinen1";
        Add(ToinenVihollinen);
        liikutettavat.Add(ToinenVihollinen);
    }


    /// <summary>
    /// Lisää kenttään Bonuspallon kutsuttaessa 
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    public void LisaaBonus(Vector paikka, double leveys, double korkeus)
    {
        // Luodaan Bonuspiste pallolle attribuutit
        PhysicsObject Bonus = PhysicsObject.CreateStaticObject(leveys, korkeus);
        Bonus.IgnoresCollisionResponse = true;
        Bonus.Position = paikka;
        Bonus.Image = pallonKuva;
        Bonus.Tag = "spallo";
        Bonus.LifetimeLeft = TimeSpan.FromSeconds(5.0);
        Add(Bonus);
    }


    /// <summary>
    /// Lisää peliin elämäpallon kutsuttaessa.
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    public void LisaaElamaPallo(Vector paikka, double leveys, double korkeus)
    {
        // Luodaan Bonuspiste pallolle attribuutit ja sijainti 
        PhysicsObject elamapallo = PhysicsObject.CreateStaticObject(leveys, korkeus);
        elamapallo.IgnoresCollisionResponse = true;
        elamapallo.Position = paikka;
        elamapallo.Image = elamaKuva;
        elamapallo.Tag = "pupallo1";
        elamapallo.LifetimeLeft = TimeSpan.FromSeconds(5.0);
        Add(elamapallo);
    }


    /// <summary>
    /// Kun pelaaja törmää viholliseen aliohjelma tuhoaa vihollisen ja vähentää pelaajalta pisteitä.
    /// </summary>
    /// <param name="hahmo"></param>
    /// <param name="Vihollinen"></param>
    public void TormaaViholliseen(PhysicsObject hahmo, PhysicsObject Vihollinen)
    {
        // Kun pelaaja törmää viholliseen häneltä vähennetään viisi pistettä ja elämä. Vaikeimmalla menettää 10 pistettä. 
        // Lisäksi peli loppuu kun elämät menevät nollaan
        Vihollinen.Destroy();
        osuma.Play();
        elamalaskuri.Value -= 1;
        if (Vaikeustaso == 3) { pisteLaskuri.Value -= 10; } else pisteLaskuri.Value -= 5;
        if (elamalaskuri == 0) { kuolema.Play(); MessageDisplay.Add("Voima on kanssasi"); PelaajaHavisi(); }
    }


    /// <summary>
    /// Pelaajan törmätessä bonuspalloon pallo tuhoutuu ja lisää pelaajalle pisteitä.
    /// </summary>
    /// <param name="hahmo"></param>
    /// <param name="bonus"></param>
    public void TormaaBonukseen(PhysicsObject hahmo, PhysicsObject bonus)
    {
        //Kun pelaaja osuu bonuspalloon, saa hän 10 pistettä ja pallo tuhoutuu
        bonus.Destroy();
        sipallo.Play();
        pisteLaskuri.Value += 10;
    }


    /// <summary>
    /// Pelaajan törmätessä elämäpalloon pelaajalle lisätään elämiä vaikeustasosta riippuen.
    /// </summary>
    /// <param name="hahmo"></param>
    /// <param name="elama"></param>
    public void TormaaElamaan(PhysicsObject hahmo, PhysicsObject elama)
    {
        //Pelaajan osuessa elämäpalloon hän saa vaikeustasosta riippuen elämiä lisää ja pallo häviää
        elama.Destroy();
        Sound punpallo = sipallo.CreateSound();
        punpallo.Pitch = 1.5;
        punpallo.Volume = 10.0;
        punpallo.Play();
        if (Vaikeustaso == 1)
        {
            elamalaskuri.Value += 2;
        }
        else elamalaskuri.Value += 1;
    }


    /// <summary>
    /// Aliohjelma luo ammuksen kun pelaaja ampuu
    /// </summary>
    /// <param name="ase"></param>
    public void AmmuAseella(PlasmaCannon ase)
    {
        // Kun ammutaan lähtee plasmapallo
        PhysicsObject ammus = pelaajanAse.Shoot();
    }


    /// <summary>
    /// Aliohjelma tuhoaa ammuksen, kohteen ja lisää pelaajalle pisteen.
    /// </summary>
    /// <param name="ammus"></param>
    /// <param name="Asia"></param>
    public void AmmusOsui(PhysicsObject ammus, PhysicsObject Asia)
    {
        // Kun plasmapallo osuu vihollisee, palloon tai maastoon se tuhoaa sen mihin osuu.
        // HUOM! kun ampuu pistepallon saa vain yhden pisteen
        ammus.Destroy();
        Asia.Destroy();
        rajahdys();
        kuolema.Play();
        pisteLaskuri.Value += 1;
    }


    /// <summary>
    /// Aliohjelma luo räjähdyksen kohteen tuhoutuessa
    /// </summary>
    public void rajahdys()
    {
        // Kuoleman yhteydessä tapahtuvan räjähdyksen parametrien määritystä
        int pMaxMaara = 1;
        ExplosionSystem rajahdys = new ExplosionSystem(LoadImage("rajahdys2"), pMaxMaara);
        Add(rajahdys);

        double x = 0;
        double y = 0;
        int pMaara = 50;
        rajahdys.AddEffect(x, y, pMaara);
    }


    /// <summary>
    /// Aliohjelma kännistää aika- pistelaskurin kun vaikeustaso on valittu.
    /// </summary>
    public void LuoAikaJaPisteLaskuri()
    {
        //Luodaan ajastin joka alkaa 30 sekunnista ja valuu nollaan päin.
        alaspainLaskuri = new DoubleMeter(30);
        aikaLaskuri = new Timer();
        aikaLaskuri.Interval = 0.1;
        aikaLaskuri.Timeout += LaskeAlaspain;
        aikaLaskuri.Start();


        // Miltä ajastin näyttää
        Label aikaNaytto = new Label();
        aikaNaytto.TextColor = Color.White;
        aikaNaytto.X = Screen.Left + 25;
        aikaNaytto.Y = Screen.Top - 15;
        aikaNaytto.DecimalPlaces = 1;
        aikaNaytto.BindTo(alaspainLaskuri);
        Add(aikaNaytto);

        //Luodaan pistelaskuri, sille ulkonäkö ja sijainti
        pisteLaskuri = new IntMeter(0);

        Label pisteNaytto = new Label();
        pisteNaytto.X = Screen.Left + 60;
        pisteNaytto.Y = Screen.Top - 40;
        pisteNaytto.TextColor = Color.White;
        pisteNaytto.Color = Color.Black;
        pisteNaytto.Title = "Pisteet";

        pisteNaytto.BindTo(pisteLaskuri);
        Add(pisteNaytto);
    }

    /// <summary>
    /// Aliohjelma laskee sekuntikelloa alaspäin ja kutsuu seuraavaa aaltoa edellisen loppuessa
    /// </summary>
    public void LaskeAlaspain()
    {
        //Kuinka nopeasti ajastin laskee aikaa ja mitä tapahtuu kun se on nolla. 
        alaspainLaskuri.Value -= 0.1;

        // Kun ajastin on nolla aloitetaan seuraava aalto ja asetetaan ajastimen arvoksi jälleen 30 sekuntia.
        if (alaspainLaskuri.Value == 0)
        {
            IsPaused = true;
            alaspainLaskuri.Value = 30;
            Aaltolaskuri.Value += 1;
            MultiSelectWindow valiValikko = new MultiSelectWindow("Seuraava aalto: " + Aaltolaskuri.Value, "Seuraava Aalto", "Lopeta");
            Add(valiValikko);
            valiValikko.AddItemHandler(0, SeuraavaAalto);
            valiValikko.AddItemHandler(1, Exit);
        }
    }


    /// <summary>
    /// Luo aaltolaskurin
    /// </summary>
    public void LuoAaltoLaskuri()
    {
        // Aaltolaskuri pysyy kärryillä, siitä mones aalto on menossa
        Aaltolaskuri = new IntMeter(1);
    }


    /// <summary>
    /// Luo vaikeustasolle laskurin, jonka avulla ohjelma tunnistaa, millä vaikeustasolla pelaaja pelaa.
    /// </summary>
    public void LuoVaikeus()
    {
        Vaikeustaso = new IntMeter(0);
    }


    /// <summary>
    /// Luo pelaajalle elämät vaikeustason mukaan ja asettaa sen näkyville vasempaan ylänurkkaan
    /// </summary>
    public void Elama()
    {
        // Elämälaskuri laskee pelaajan elämäpisteet ja näyttää ne ruudun vasemmassa yläreunassa
        // Elämän määrä riippuu vaikeustasosta
        if (Vaikeustaso == 1)
        {
            elamalaskuri = new IntMeter(10);
        }
        else if (Vaikeustaso == 2)
        {
            elamalaskuri = new IntMeter(5);
        }
        else elamalaskuri = new IntMeter(3);
        Label elama = new Label();
        elama.X = Screen.Left + 60;
        elama.Y = Screen.Top - 70;
        elama.TextColor = Color.White;
        elama.Color = Color.Black;
        elama.Title = "Elämät";
        elama.BindTo(elamalaskuri);
        Add(elama);
    }


    /// <summary>
    /// Ikkuna, joka tulee esiin pelaajan hävitessä ja pelin loppuessa
    /// </summary>
    public void PelaajaHavisi()
    {
        //Kun pelaaja häviää se tuhoutuu ja näytetään highscore-taulukko
        pelaaja.Destroy();
        omakuolema.Play();
        MultiSelectWindow Kuolema = new MultiSelectWindow("Yritä uudelleen?", "Kyllä", "Ei");
        Add(Kuolema);
        Kuolema.AddItemHandler(0, YritaUudelleen);
        Kuolema.AddItemHandler(1, Exit);
        topLista.EnterAndShow(pisteLaskuri.Value);
        //topLista.HighScoreWindow.Closed += AloitaPeli;
    }


    /// <summary>
    /// Ikkuna, joka tulee näkyviin pelaajan hävitessä. Aliohjelma nollaa kaiken ja käynnistää pelin alusta.
    /// </summary>
    public void YritaUudelleen()
    {
        // Lopettaa kaiken ja alottaa pelin alusta
        ClearAll();
        Begin();
    }


    /// <summary>
    /// Luo highscore ikkunan
    /// </summary>
    public void Highscore()
    {
        //Highscore-taulukon luonti. Kun ikkuna suljetaan pelaaja viedään päävalikkoon
        topLista.EnterAndShow(pisteLaskuri.Value);
        topLista.HighScoreWindow.Closed += AloitaPeli;
    }


    /// <summary>
    /// Aliohjelmaa kutsutaan kun, highscore lista suljetaan. Aliohjelma nollaa kaiken ja kynnistää pelin alusta.
    /// </summary>
    /// <param name="sender"></param>
    public void AloitaPeli(Window sender)
    {
        // Kun peliä jatketaan otetaan samalla pause pois päältä
        IsPaused = false;
        ClearAll();
        Begin();
    }


    /// <summary>
    /// Aliohjelma luo ikkunan, josta pelaaja voi valita vaikeuasteen
    /// </summary>
    public void ValitseHaastavuus()
    {
        MultiSelectWindow Vaikeus = new MultiSelectWindow("Valitse vaikeusaste", "Nössö", "Tavis", "Fyysikko");
        Add(Vaikeus);
        Vaikeus.AddItemHandler(0, VaikeusasteHelppo);
        Vaikeus.AddItemHandler(1, VaikeusasteNormaali);
        Vaikeus.AddItemHandler(2, VaikeusasteVaikea);
    }


    /// <summary>
    /// Aliohjelmaa kutsutaan, kun pelaaja valitsee vaikeusasteeksi nössön.
    /// </summary>
    public void VaikeusasteHelppo()
    {
        Vaikeustaso.Value += 1;
        IsPaused = false;
        Elama();
        LuoAikaJaPisteLaskuri();
    }


    /// <summary>
    /// Aliohjelmaa kutsutaan, kun pelaaja valitsee vaikeusasteeksi taviksen.
    /// </summary>
    public void VaikeusasteNormaali()
    {
        Vaikeustaso.Value += 2;
        IsPaused = false;
        Elama();
        LuoAikaJaPisteLaskuri();
    }


    /// <summary>
    /// Aliohjelmaa kutsutaan, kun pelaaja valitsee vaikeusasteeksi fyysikon.
    /// </summary>
    public void VaikeusasteVaikea()
    {
        Vaikeustaso.Value += 3;
        IsPaused = false;
        Elama();
        LuoAikaJaPisteLaskuri();
    }
}
