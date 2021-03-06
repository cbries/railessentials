# EcosApp

Steckdosen:
192.168.178.62:81
{
	in1: 1 | 0, 
	in2: 1 | 0,
	in3: 1 | 0,
	in4: 1 | 0 
}

RGB-Tageslicht
192.168.178.66:81
{
	r: 0 - 255,
	g: 0 - 255,
	b: 0 - 255,
	w: 0 - 255
}

# Important flags to reduce UI-updates

```
window.__ecosbaseChanged = window.ecosData.ecosbaseChanged;
window.__locomotivesChanged = window.ecosData.__locomotivesChanged;
window.__accessoriesChanged = window.ecosData.accessoriesChanged;
window.__feedbacksChanged = window.ecosData.feedbacksChanged;
```
