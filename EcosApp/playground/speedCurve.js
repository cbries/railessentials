class SpeedCurve {
	/*
	 * speedMode [dcc14, dcc28, dcc128, mm14, mm27, mm128]
	 * speedtimeMax
	 * speedstepMax
	 * data (preloadData)
	 * onChanged := callback called at any time when some data changed
	 */
	constructor(options = {}) {
		
		this.__speedCfgs = {
			"dcc14": { speedsteps: 14, extraWidthForSteps: 2, noobyWidth: 4, noobyHeight: 4, deltaShow: 1 },
			"dcc28": { speedsteps: 28, extraWidthForSteps: 2, noobyWidth: 4, noobyHeight: 4, deltaShow: 2 },
			"dcc128": { speedsteps: 128, extraWidthForSteps: 2, noobyWidth: 4, noobyHeight: 4, deltaShow: 4 },
			"mm14": { speedsteps: 14, extraWidthForSteps: 2, noobyWidth: 4, noobyHeight: 4, deltaShow: 1 },
			"mm27": {	speedsteps: 27, extraWidthForSteps: 0,	noobyWidth: 4,	noobyHeight: 4, deltaShow: 2 },
			"mm128": { speedsteps: 128, extraWidthForSteps: 2, noobyWidth: 4, noobyHeight: 4, deltaShow: 5 }
		};
		
		this.__speedMode = this.__speedCfgs.dcc28;
				
		if(typeof options !== "undefined" && options != null) {
			if(typeof options.speedMode !== "undefined" && options.speedMode != null)
				this.__speedMode = this.__speedCfgs[options.speedMode];
		}
		
		this.__speedCurveContainerClass = ".speedCurveContainer";
		this.__speedCurveRootClass = ".speedCurveRoot";

		this.__speedCurveContainer = $(this.__speedCurveContainerClass);
		this.__createControls();
		this.__speedCurveRoot = $(this.__speedCurveRootClass);
		this.__chkLabelShow = this.__speedCurveContainer.find("#chkLabelShow");

		this.__recentMouseMoveCoord = { x: 0, y: 0, topPage: 0, leftPage: 0 };
		this.__maxWidth = 750;
		this.__maxHeight = 200;
		this.__mouseDown = false;

		this.__speedTimeMaxDefault = 5;
		this.__speedStepMaxDefault = parseInt(this.__speedMode.speedsteps / 2);
		
		this.__preloadData = [];
		
		//
		// apply user options
		//
		if(typeof options !== "undefined" && options != null) {
			if(typeof options.speedtimeMax !== "undefined" && options.speedtimeMax != null)
				this.__speedTimeMaxDefault = options.speedtimeMax;
			if(typeof options.speedstepMax !== "undefined" && options.speedstepMax != null)
				this.__speedStepMaxDefault = options.speedstepMax;				
			if(typeof options.data !== "undefined" && options.data != null && data.length > 0)
				this.__preloadData = data;
			if(typeof options.onChanged !== "undefined" && options.onChanged != null)
				this.__onChanged = options.onChanged;
		}
	}
	
	__createControls() {
		const self = this;
		self.__speedCurveContainer.append(
		'<div class="speedCurveControls">' +
		'Preloads:' + 
		'<button id="cmdSpeedLinear">Linear</button>' +
		'<button id="cmdSpeedExponential">Exponential</button>' +
		'<input type="checkbox" id="chkLabelShow" name="chkLabelShow" checked>' +
		'<label for="chkLabelShow"> Show Labels</label>' +
		'<div style="padding-top: 10px;">' +
		'Speedstep (max): <select id="cmdSpeedMax"></select>' +
		'</div>' +
		'<div style="padding-top: 10px;">' +
		'Time (max): <select id="cmdSpeedTimeMax"></select>' +
		'</div>' +
	  '</div>');
	}
	
	install() {
		const self = this;
		document.body.onmousedown = function() { self.__mouseDown = true; }
	  document.body.onmouseup = function() { self.__mouseDown = false; }
	
		window.addEventListener("resize", function(){
			self.__redrawSpeedDots(false);
			self.__realignLines();
		});
	
		self.__initDiagram();		
		
		//
		// TODO apply this.__preloadData
		//
	}
	
	__initDiagram() {
	  const self = this;
		const speedCurveContainer = self.__speedCurveContainer;
	
		speedCurveContainer.css({ 
			width: self.__maxWidth + "px", 
			height: self.__maxHeight + "px"
		});
		
		//
		// add horizontally / vertically line for bette recognization
		//
		self.__lineSpeed = $('<div>') // horizontally
			.css({ width: this.__maxWidth + "px" })
			.addClass('speedCurveLineSpeed');		
		self.__lineSpeed.appendTo(this.__speedCurveRoot);
			
		self.__lineTime = $('<div>') // vertically
			.css({ height: this.__maxHeight + "px" })
			.addClass('speedCurveLineTime');
		self.__lineTime.appendTo(this.__speedCurveRoot);		
		
		//
		// add noobys
		//
		const parentOffsetTop = speedCurveContainer.get(0).getBoundingClientRect().top;
		const parentOffsetLeft = speedCurveContainer.get(0).getBoundingClientRect().left;
		const xsteps_tmp = self.__maxWidth / self.__speedMode.speedsteps;
		const xsteps = (self.__maxWidth + xsteps_tmp - (self.__speedMode.noobyWidth / 2) + self.__speedMode.extraWidthForSteps ) / self.__speedMode.speedsteps;
		let elementsToAppend = $();		
		for(let i=0; i < self.__speedMode.speedsteps; ++i) {
			const nooby = $("<div>")
				.css({
					width: self.__speedMode.noobyWidth + "px",
					height: self.__speedMode.noobyHeight + "px"
				})
				.data("index", i)
				.data("xsteps", xsteps)
				.addClass("nooby_" + i)
				.addClass("nooby");
				
				const noobySpeedLbl = $('<div>').addClass('noobySpeedLbl').html("");
				noobySpeedLbl.appendTo(nooby);
				
				let txtTime = "";
				const noobyTimeLbl = $('<div>').addClass('noobyTimeLbl').html(txtTime);
				noobyTimeLbl.appendTo(nooby);
				
				elementsToAppend = elementsToAppend.add(nooby);
		}
		speedCurveContainer.append(elementsToAppend);
		self.__redrawSpeedDots();
		
		const selectSpeedCtrl = speedCurveContainer.find('select#cmdSpeedMax');
		for(let i = 0; i < self.__speedMode.speedsteps; ++i) {
			const opt = $('<option>', {value: i}).html(i);
			opt.appendTo(selectSpeedCtrl);
		}
		selectSpeedCtrl.change(function(ev) {
			const v = $(this).val();
			self.__highlightMaxSpeed(v);
			self.__realignLines();
		});
		selectSpeedCtrl.val(this.__speedStepMaxDefault);
		
		const selectTimeCtrl = speedCurveContainer.find('select#cmdSpeedTimeMax');
		for(let i = 0; i < self.__speedMode.speedsteps; ++i) {
			if(i === self.__speedTimeMaxDefault) {
				const opt = $('<option>', {value: i, selected: ''}).html(i + "s");
				opt.appendTo(selectTimeCtrl);	
			} else {
				const opt = $('<option>', {value: i}).html(i + "s");
				opt.appendTo(selectTimeCtrl);				
			}
		}
		selectTimeCtrl.change(function(ev) {
			self.__realignLines();
		});	
		
		speedCurveContainer.mouseleave(function(ev) {
			self.__mouseDown = false;
		});	
		
		speedCurveContainer.mousemove(function(ev) {
			ev.preventDefault();
			const coord = self.__getMouseCoordRelativeTo(this, ev);
			self.__recentMouseMoveCoord = coord;
			if(self.__mouseDown === false) return;	
			self.__handleMouseClickMove(coord);
		});
		
		speedCurveContainer.click(function(ev){	
			self.__handleMouseClickMove(self.__recentMouseMoveCoord);			
		});
	
		const cmdSpeedLinear = speedCurveContainer.find("#cmdSpeedLinear");
		const cmdSpeedExponential = speedCurveContainer.find("#cmdSpeedExponential");		
				
		cmdSpeedLinear.click(function(ev){
			self.__preloadLinear();
		});
		cmdSpeedExponential.click(function(ev){
			self.__preloadExponential();
		});
		this.__chkLabelShow.click(function() {
			const elementsSpeed = $('.noobySpeedLbl');
			for(let i = 0; i < elementsSpeed.length; ++i) {
				const el = $(elementsSpeed[i]);
				if(this.checked === true) el.show();
				else el.hide();
			}
			
			const elementsTime = $('.noobyTimeLbl');
			for(let i = 0; i < elementsTime.length; ++i) {
				const el = $(elementsTime[i]);
				if(this.checked === true) el.show();
				else el.hide();
			}
		});

		self.__realignLines();
		
		if(this.__preloadData.length == 0) {
			self.__preloadLinear();
		}
	}
	
	__realignLines() {
		const self = this;
		const speedCurveRoot = self.__speedCurveRoot;
		const speedCurveContainer = self.__speedCurveContainer;
		const selectSpeedCtrl = speedCurveContainer.find('select#cmdSpeedMax');
		const selectTimeCtrl = speedCurveContainer.find('select#cmdSpeedTimeMax');

		// align speed line
		const maxSpeed = parseInt(selectSpeedCtrl.val());
		const speedNooby = $('.nooby_' + maxSpeed);
		let currentY = parseInt(speedNooby.css("top").replace("px", ""));
		currentY += self.__speedMode.noobyHeight / 2;
		self.__lineSpeed.css({"top": currentY});
		
		// align time stuff
		const maxTime = parseInt(selectTimeCtrl.val());
		let currentX = parseInt(speedNooby.css("left").replace("px", ""));
		currentX += self.__speedMode.noobyWidth / 2;
		self.__lineTime.css({"left": currentX});
		
		const rect = speedCurveRoot.get(0).getBoundingClientRect();
		const bottom = rect.top + rect.height;
		
		//
		// hide time labels which are higher as the selected max time value
		//
		const isShowChecked = this.__chkLabelShow.is(":checked");
		const stepTime = maxTime / maxSpeed;
		let counterTime = 0;
		const elements = $('.nooby');		
		for(let i = 0; i < elements.length; ++i) {
			const el = $(elements[i]);
			const elTimeLbl = el.find('.noobyTimeLbl');
			const elSpeedLbl = el.find('.noobySpeedLbl');
			
			if(i > maxSpeed) {
				elTimeLbl.hide();
				elSpeedLbl.hide();				
				el.data("speed", 0);
				el.data("timeStep", 0);
			} else {
				let t = counterTime.toString();
				t = t.substr(0, 3);
				elTimeLbl.html(t+"s");
				if(isShowChecked == true)
					elTimeLbl.show();
			
				const elRect = el.get(0).getBoundingClientRect();
				const y = bottom - elRect.top;
				const factor = y / self.__maxHeight;
				const speed = parseInt(factor * self.__speedMode.speedsteps);			
				elSpeedLbl.html(speed);
				
				el.data("speed", speed);
				el.data("timeStep", counterTime);
			}
			
			counterTime += stepTime;
		}
		
		if(typeof self.__onChanged !== "undefined" && self.__onChanged != null)
			self.__onChanged();
	}
	
	getData() {
		const self = this;;
		const speedCurveRoot = self.__speedCurveRoot;
		const speedCurveContainer = self.__speedCurveContainer;
		const selectSpeedCtrl = speedCurveContainer.find('select#cmdSpeedMax');
		const selectTimeCtrl = speedCurveContainer.find('select#cmdSpeedTimeMax');
		
		const maxSpeed = parseInt(selectSpeedCtrl.val());
		const maxTime = parseInt(selectTimeCtrl.val());
		
		const elements = $('.nooby');
		let elAr = [];
		for(let i = 0; i < elements.length; ++i) {
			const el = $(elements[i]);
			const speed = el.data("speed");
			const timeStep = el.data("timeStep");
			
			if(timeStep === 0) continue;
			
			const itm = { speed: speed, timeStep: timeStep };
			elAr.push(itm);
		}
		
		let data =  {
			maxSpeed: maxSpeed,
			maxTime: maxTime,
			steps: elAr
		};		
		
		return data;
	}
	
	__highlightMaxSpeed(idx) {
		const self = this;
		const elements = $('.nooby');
		const iidx = parseInt(idx);
		for(let i=0; i < elements.length; ++i) {
			const el = $(elements[i]);
			el.removeClass("noobyMax");
			if(i === iidx) 
				el.addClass("noobyMax");
		}
	}
	
	__preloadLinear() {
		const self = this;
		const elements = $('.nooby');
		const speedsteps = self.__speedMode.speedsteps;
		const ystep = self.__maxHeight / speedsteps;
		for(let i = 0; i < speedsteps; ++i) {
			const el = $(elements[i]);
			const y = (i*ystep);
			self.__setY(el, y);
		}
		
		self.__realignLines();
	}
	
	__preloadExponential() {
		console.log("Preload exponential!");
		const self = this;
		const elements = $('.nooby');
		const speedsteps = self.__speedMode.speedsteps;
		const deltaStep = self.__speedMode.deltaShow;
				
		const preloadEsu28 = [ 
			0, 
			2.000, 5.763, 9.573, 13.480, 17.536, 21.794, 26.306, 31.127, 36.309, 
			41.909, 47.980, 54.578, 61.759, 69.578, 78.091, 87.353, 97.423, 108.356, 
			120.208, 133.037, 146.900, 161.854, 177.956, 195.264, 213.836, 233.728, 
			255.000
		];
		
		const preloadLenz28 = [ 		
			0,
			2.000, 4.591, 7.786, 11.562, 15.905, 20.809, 26.266, 32.272, 38.822,
			45.914, 53.543, 61.706, 70.402, 79.628, 89.382, 99.662, 110.465, 121.792, 
			133.639, 146.005, 158.889, 172.290, 186.206, 200.636, 215.579, 231.034, 
			247.000
		];
					
		const values = preloadEsu28;
					
		const maxI = values.length;
							
		for(let i = 0, elIdx = 0; i < maxI - 1; ++i, elIdx += deltaStep) {
			const el = $(elements[elIdx]);
			
			const v0 = values[i];
			const v1 = values[i+1];
			
			let maxSub = elIdx + deltaStep;
			if(i === maxI - 2) {
				maxSub = speedsteps;
			}
						
			const el_delta = maxSub - elIdx;
			const v_delta = v1 - v0;
			const factor = v_delta / el_delta;
						
			for(let j=elIdx, multi = 0; j < maxSub; ++j, ++multi) {
				const v_ = v0 + (factor*multi);
				const ell = $(elements[j]);
				const y = (self.__maxHeight/255) * v_;
								
				self.__setY(ell, y);
			}		
		}
		
		self.__realignLines();
	}
	
	__getOffset() {
		const self = this;
		const speedCurveContainer = self.__speedCurveContainer;
		const rect = speedCurveContainer.get(0).getBoundingClientRect();
		const parentOffsetTop = rect.top;
		const parentOffsetLeft = rect.left;
		const parentOffsetHeight = rect.height;		
		const newTop = parentOffsetTop + parentOffsetHeight - (self.__speedMode.noobyHeight / 2);
		const newLeft = (parentOffsetLeft - (self.__speedMode.noobyWidth / 2-1));
		return { top: newTop, left: newLeft };
	}
	
	__setY(el, y) {
		const self = this;
		const yoffset = self.__getOffset().top;
		y = yoffset - y;		
		el.css({ top: y + "px" });
	}
	
	__redrawSpeedDots(initMode = true) {
		const self = this;	
		const offset = self.__getOffset();
		
		const elements = $('.nooby');
		for(let i = 0; i < elements.length; ++i) {
			const el = $(elements[i]);
			const idx = el.data("index");
			const xsteps = el.data("xsteps");
			
			const left = offset.left + parseInt(i*xsteps);
			
			if(initMode == true) {			
				el.css({ top: offset.top + "px", left: left + "px" });
			} else {
				el.css({ left: left + "px" });
			}
		}
	}
	
	__handleMouseClickMove(coord) {
		const self = this;
		const steps = self.__maxWidth / self.__speedMode.speedsteps;
		const idx = self.__getIndexByWidth(coord.x, self.__maxWidth, steps);
		if(idx < 0) return;		
		const noobyEl = $('.nooby_' + idx);
		if(typeof noobyEl === "undefined" || noobyEl == null || noobyEl.length === 0) 
			return;		
			
		const speedCurveContainer = self.__speedCurveContainer;
		const parentOffsetTop = speedCurveContainer.get(0).getBoundingClientRect().top;
		const parentHeight = speedCurveContainer.get(0).getBoundingClientRect().height;
		const maxY = parentOffsetTop + parentHeight - self.__speedMode.noobyHeight;;
			
		if(coord.topPage > maxY) return;
			
		noobyEl.css({
			top: coord.topPage + "px"
		});
		
		self.__realignLines();
	}
	
	__getMouseCoordRelativeTo(rootElement, ev){
		const self = this;
		const evX = ev.pageX;
		const evY = ev.pageY;		
		const parentOffsetTop = rootElement.getBoundingClientRect().top;
		const parentOffsetLeft = rootElement.getBoundingClientRect().left;	
		return {
			x: evX - parentOffsetLeft,
			y: evY - parentOffsetTop,
			topPage: evY,
			leftPage: evX
		};
	}
	
	__getIndexByWidth(x, width, step) {
		if(x < 0) return -1;
		if(x > width) return -1;
		let idx = 0;
		for(let i = 0; i < width; i += step) {
			const lowX = i;
			const highX = lowX + step;
			if(x >= lowX && x < highX)
				return idx;
			++idx;
		}
		return -1;
	}
}