/*
<script>
var ForgeWebSocket = {
	socket: null,
	rawData: []
};

Module.onRuntimeInitialized = function() {
	_socket = function() { };
	function SendMessage(gameObject, func, param) {
		if (param === undefined) {
			if (typeof this.SendMessage_vss != 'function') {
				this.SendMessage_vss = Module.cwrap('SendMessage', 'void', ['string', 'string']);
			}
			
			this.SendMessage_vss(gameObject, func);
		} else if (typeof param === "string") {
			if (typeof this.SendMessage_vsss != 'function') {
				this.SendMessage_vsss = Module.cwrap('SendMessageString', 'void', ['string', 'string', 'string']);
			}
			
			this.SendMessage_vsss(gameObject, func, param);
		} else if (typeof param === "number") {
			if (typeof this.SendMessage_vssn != 'function') {
				this.SendMessage_vssn = Module.cwrap('SendMessageFloat', 'void', ['string', 'string', 'number']);
			}
			
			this.SendMessage_vssn(gameObject, func, param);
		} else {
			throw "" + param + " is does not have a type which is supported by SendMessage.";
		}
	}
};
</script>
*/

var ForgeNetworking = {
	ForgeConnect : function(hostPtr, port) {
		var host = Pointer_stringify(hostPtr);
		try {
			ForgeWebSocket.socket = new WebSocket("ws://" + host + ":" + port + "/");
			ForgeWebSocket.socket.binaryType = "arraybuffer";
		} catch (e) {
			alert("Your browser currently doesn't support web sockets, please upgrade your browser!");
			return;
		}
		
		ForgeWebSocket.socket.onopen = function(event) {
			// Ping the server for player identity
			this.send(" ");
		};
		
		ForgeWebSocket.socket.onerror = function(event) {
			ForgeWebSocket.runningConnect = false;
		};
		
		ForgeWebSocket.socket.onmessage = function(event) {
			ForgeWebSocket.rawData.push(event.data);
		};
	},
	ForgeWrite: function(data, length) {
		try {
			var d2 = new Uint8Array(HEAPU8.buffer, data, length);
			ForgeWebSocket.socket.send(d2);
		} catch (e) {
			
		}
	},
	ForgeClose: function() {
		ForgeWebSocket.socket.close()
	},
	ForgeShiftDataRead: function() {
		var data = ForgeWebSocket.rawData.shift();
		var ptr = _malloc(data.byteLength);
		var dataHeap = new Uint8Array(HEAPU8.buffer, ptr, data.byteLength);
		dataHeap.set(new Uint8Array(data));
		return ptr;
	},
	ForgeContainsData: function() {
		if (!ForgeWebSocket.rawData.length) {
			return 0;
		}
		
		return ForgeWebSocket.rawData[0].byteLength;
	},
	ForgeLog: function(data) {
		console.log(Pointer_stringify(data));
	}
};
mergeInto(LibraryManager.library, ForgeNetworking);