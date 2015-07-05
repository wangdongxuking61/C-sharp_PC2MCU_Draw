//send fireImage camera Data
void SCI_Send_fireImage(UARTn uratn)
{
	int H = 240, L = 320;
	//send "CAMERA"
	uart_putchar(uratn, 'C');
	uart_putchar(uratn, 'A');
	uart_putchar(uratn, 'M');
	uart_putchar(uratn, 'E');
	uart_putchar(uratn, 'R');
	uart_putchar(uratn, 'A');
	uart_putchar(uratn, (unsigned char)(H >> 8));//send Hang
	uart_putchar(uratn, (unsigned char)(H & 0x00FF));
	uart_putchar(uratn, (unsigned char)(L >> 8));//send Lie
	uart_putchar(uratn, (unsigned char)(L & 0x00FF));
	//send all data
	for (int i = 0; i < H; i++)
	for (int j = 0; j < L / 8; j++)
		uart_putchar(uratn, Image_fire[i][j]);
}


//send usual camera Data
void SCI_Send_Image(UARTn uratn)
{
	int H = 240, L = 320;
	//send "CAMERA"
	uart_putchar(uratn, 'C');
	uart_putchar(uratn, 'A');
	uart_putchar(uratn, 'M');
	uart_putchar(uratn, 'E');
	uart_putchar(uratn, 'R');
	uart_putchar(uratn, 'A');
	//send Hang
	uart_putchar(uratn, (unsigned char)(H >> 8));
	uart_putchar(uratn, (unsigned char)(H & 0x00FF));
	//send Lie
	uart_putchar(uratn, (unsigned char)(L >> 8));
	uart_putchar(uratn, (unsigned char)(L & 0x00FF));
	//send all data
	for (int i = 0; i < H; i++)
	for (int j = 0; j < L; j++)
		uart_putchar(uratn, cameraData[i][j]);
}