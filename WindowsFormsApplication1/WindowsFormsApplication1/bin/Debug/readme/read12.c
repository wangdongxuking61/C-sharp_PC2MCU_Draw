//send fireImage camera Data
void SCI_Send_fireImage()
{
	int i, j;
	int H = 240, L = 320;
	//send "CAMERA"
	while (AS1_SendChar('C'));
	while (AS1_SendChar('A'));
	while (AS1_SendChar('M'));
	while (AS1_SendChar('E'));
	while (AS1_SendChar('R'));
	while (AS1_SendChar('A'));
	while (AS1_SendChar((unsigned char)(H >> 8)));//send Hang
	while (AS1_SendChar((unsigned char)(H & 0x00FF)));
	while (AS1_SendChar((unsigned char)(L >> 8)));//send Lie
	while (AS1_SendChar((unsigned char)(L & 0x00FF)));
	//send all data
	for (i = 0; i < H; i++)
	for (j = 0; j < L / 8; j++)
		while (AS1_SendChar(Image_fire[i][j]));
}


//send usual camera Data
void SCI_Send_Image()
{
	int i, j;
	int H = 240, L = 320;
	//send "CAMERA"
	while (AS1_SendChar('C');
	while (AS1_SendChar('A');
	while (AS1_SendChar('M');
	while (AS1_SendChar('E');
	while (AS1_SendChar('R');
	while (AS1_SendChar('A');
	//send Hang
	while (AS1_SendChar((unsigned char)(H >> 8)));
	while (AS1_SendChar((unsigned char)(H & 0x00FF)));
	//send Lie
	while (AS1_SendChar((unsigned char)(L >> 8)));
	while (AS1_SendChar((unsigned char)(L & 0x00FF)));
	//send all data
	for (i = 0; i < H; i++)
	for (j = 0; j < L; j++)
		while (AS1_SendChar(cameraData[i][j]));
}