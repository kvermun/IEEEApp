function FindProxyForURL(url, host)
{
	var lcheck=host.split(".");
	if(lcheck[0]=="10" ||lcheck[0]=="144" || lcheck[0]=="127" || host=="localhost")
		return "DIRECT";
	var list=["10.3.100.209","10.3.100.210","10.3.100.211","10.3.100.212","144.16.192.247","144.16.192.216","144.16.192.217","144.16.192.218","144.16.192.245","144.16.192.213"];
	var index=0;
	for(var i=3, j=1;i>0;i--,j*=2){
		index+=(Math.floor(Math.random()*100)%2)*j;
	}
    return "PROXY "+list[index]+":8080; PROXY "+list[list.length-1]+":8080";
}