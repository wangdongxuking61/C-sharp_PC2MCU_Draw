//send ccd Data
void SCI_Send_CCD(UARTn uratn, int ccdNum)
{
	if (ccdNum != 1 && ccdNum != 2)
		return;
	uart_putchar(uratn, 255);
	uart_putchar(uratn, 255);
	uart_putchar(uratn, ccdNum);//number
	for (int i = 0; i < 128; i++)
	if (ccdData1[i] == 255)
		uart_putchar(uratn, 254);
	else
		uart_putchar(uratn, ccdData1[i]);
	if (ccdNum == 2)
	for (int i = 0; i < 128; i++)
	if (ccdData2[i] == 255)
		uart_putchar(uratn, 254);
	else
		uart_putchar(uratn, ccdData2[i]);
}